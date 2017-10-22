using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using UnityEngine;

namespace f3
{

    public class DrawPrimitivesToolBuilder : IToolBuilder
    {
        public bool IsSupported(ToolTargetType type, List<SceneObject> targets) {
            return (type == ToolTargetType.Scene);
        }

        public ITool Build(FScene scene, List<SceneObject> targets) {
            return new DrawPrimitivesTool(scene);
        }
    }




    public class DrawPrimitivesTool : ITool
    {
        static readonly public string Identifier = "draw_primitives";

        FScene scene;

        // primitives will not be created with a smaller dimension than this
        public float MinDimension = 0.01f;
        public float MaxDimension = 9999.0f;

        virtual public string Name {
            get { return "DrawPrimitives"; }
        }
        virtual public string TypeIdentifier {
            get { return Identifier; }
        }


        InputBehaviorSet behaviors;
        virtual public InputBehaviorSet InputBehaviors {
            get { return behaviors; }
            set { behaviors = value; }
        }

        public virtual bool AllowSelectionChanges { get { return true; } }


        public DrawPrimitivesTool(FScene scene)
        {
            this.scene = scene;

            behaviors = new InputBehaviorSet();

            // TODO is this where we should be doing this??
            behaviors.Add(
                new DrawPrimitivesTool_MouseBehavior(scene.Context) { Priority = 5 } );
            behaviors.Add(
                new DrawPrimitivesTool_SpatialDeviceBehavior(scene.Context) { Priority = 5 });

            // shut off transform gizmo
            scene.Context.TransformManager.PushOverrideGizmoType(TransformManager.NoGizmoType);
        }



        virtual public void PreRender()
        {
            if (primitive != null)
                primitive.PreRender();
        }

        virtual public bool HasApply { get { return false; } }
        virtual public bool CanApply { get { return false; } }
        virtual public void Apply() { }


        public virtual void Setup()
        {
        }

        public void Shutdown()
        {
            scene.Context.TransformManager.PopOverrideGizmoType();
        }

        public enum SupportedTypes
        {
            Cylinder, Box, Sphere
        }
        SupportedTypes eActiveType;
        public SupportedTypes ActiveType {
            get { return eActiveType;  }
        }
        public int Steps
        {
            get { return (eActiveType == SupportedTypes.Sphere) ? 1 : 2; }
        }


        void CreateNewPrimitive()
        {
            SOType activePrimType = scene.DefaultPrimitiveType;
            if (activePrimType == SOTypes.Sphere)
                eActiveType = SupportedTypes.Sphere;
            else if (activePrimType == SOTypes.Box)
                eActiveType = SupportedTypes.Box;
            else
                eActiveType = SupportedTypes.Cylinder;


            float fScale = scene.GetSceneScale();
            if (eActiveType == SupportedTypes.Cylinder) {
                primitive = new MeshPrimitivePreview();
                primitive.Type = MeshPrimitivePreview.PrimType.Cylinder;
                primitive.Center = CenterModes.Base;
                primitive.Create(scene.NewSOMaterial, scene.RootGameObject, MinDimension*fScale);

            } else if (eActiveType == SupportedTypes.Sphere) {
                primitive = new MeshPrimitivePreview();
                primitive.Type = MeshPrimitivePreview.PrimType.Sphere;
                primitive.Center = CenterModes.Origin;
                primitive.Create(scene.NewSOMaterial, scene.RootGameObject, MinDimension*fScale);

            } else if (eActiveType == SupportedTypes.Box) {
                primitive = new MeshPrimitivePreview();
                primitive.Type = MeshPrimitivePreview.PrimType.Box;
                primitive.Center = CenterModes.Corner;
                primitive.Create(scene.NewSOMaterial, scene.RootGameObject, MinDimension*fScale);
            } else 
                throw new NotImplementedException("DrawPrimitivesTool.Create: unsupported type");

        }




        MeshPrimitivePreview primitive;
        Frame3f primStartW, primStartS;
        public Frame3f PrimFrame {
            get { return primStartW; }
        }


        public void BeginDraw_Ray(Ray3f ray, AnyRayHit rayHit, int nStep)
        {
            CreateNewPrimitive();

            Vector3f hitPos = rayHit.hitPos;

            Frame3f sceneW = scene.SceneFrame;
            if (rayHit.hitSO == null) {
                primStartW = sceneW;
                primStartW.Origin = hitPos;
            } else {
                if (scene.Context.TransformManager.ActiveFrameType == FrameType.WorldFrame) {
                    primStartW = sceneW;
                    primStartW.Origin = hitPos;

                } else if ( rayHit.hitSO is PivotSO ) {
                    primStartW = (rayHit.hitSO as PivotSO).GetLocalFrame(CoordSpace.WorldCoords);
                    primStartW.Origin = hitPos;

                } else if (rayHit.hitSO is PrimitiveSO) {
                    // align with object frame as much as possible, given that we still want
                    //  to use hit normal...
                    Frame3f objFrame = (rayHit.hitSO as PrimitiveSO).GetLocalFrame(CoordSpace.WorldCoords);
                    int nBestAxis = MathUtil.MostParallelAxis(objFrame, rayHit.hitNormal);
                    int nPerp = (nBestAxis + 1) % 3;
                    primStartW = new Frame3f(hitPos, rayHit.hitNormal, 1);
                    primStartW.ConstrainedAlignAxis(0, objFrame.GetAxis(nPerp), primStartW.Y);

                } else { 
                    primStartW = new Frame3f(hitPos, rayHit.hitNormal, 1);
                    primStartW.ConstrainedAlignAxis(1, sceneW.Y, primStartW.Y);
                }
            }
            primitive.Frame = primStartW;
            primStartS = scene.ToSceneFrame(primStartW);
        }


        // for 2-step primitives, have to save first-step ray-hit
        Vector3f plane_hit_local;

        public void UpdateDraw_Ray(Ray3f ray, int nStep)
        {
            // scene xform may have changed during steps (eg view rotation), so we
            // need to reconstruct our local frame
            Frame3f primCurW = scene.ToWorldFrame(primStartS);

            // step 1: find radius in plane
            // step 2: find height from plane
            float fY = MinDimension;
            if (nStep == 0) {
                Vector3f forwardDir = ray.Direction;
                Vector3f plane_hit = VRUtil.SafeRayPlaneIntersection(ray, forwardDir, primCurW.Origin, primCurW.Y);
                plane_hit_local = primCurW.ToFrameP(plane_hit);
            } else if (nStep == 1) {
                Vector3f plane_hit = primCurW.FromFrameP(plane_hit_local);
                Line3d l = new Line3d(plane_hit, primCurW.Y);
                fY = (float)DistLine3Ray3.MinDistanceLineParam(ray, l);
            }

            // figure out possible dimensions, clamp to ranges
            float planeX = MathUtil.SignedClamp(plane_hit_local[0], MinDimension, MaxDimension);
            float planeZ = MathUtil.SignedClamp(plane_hit_local[2], MinDimension, MaxDimension);
            float fR_plane = MathUtil.Clamp(plane_hit_local.Length, MinDimension/2, MaxDimension/2);
            fY = MathUtil.SignedClamp(fY, MinDimension, MaxDimension);

            // update frame
            primitive.Frame = primCurW;

            // update dimensions
            bool bIsCorner = (primitive.Center == CenterModes.Corner);
            float fScale = 1.0f;        // object is not in scene coordinates!
            if (primitive.Type == MeshPrimitivePreview.PrimType.Cylinder) {
                primitive.Width = (bIsCorner) ? fR_plane * fScale : 2 * fR_plane * fScale;
                primitive.Depth = primitive.Width;
                primitive.Height = fY * fScale;

            } else if (primitive.Type == MeshPrimitivePreview.PrimType.Box) {
                primitive.Width = (bIsCorner) ? planeX * fScale : 2 * planeX * fScale;
                primitive.Depth = (bIsCorner) ? planeZ * fScale : 2 * planeZ * fScale;
                primitive.Height = fY * fScale;

            } else if (primitive.Type == MeshPrimitivePreview.PrimType.Sphere) {
                primitive.Width = (bIsCorner) ? fR_plane * fScale : 2 * fR_plane * fScale;
                primitive.Depth = primitive.Width;
                primitive.Height = Mathf.Sign(fY) * primitive.Width;

            } else
                throw new NotImplementedException("DrawPrimitivesTool.UpdateDraw_Ray - type not supported");
        }




        public void BeginDraw_Spatial(Ray3f ray, AnyRayHit rayHit, Frame3f handFrame, int nStep)
        {
            BeginDraw_Ray(ray, rayHit, nStep);
        }



        public void UpdateDraw_Spatial(Ray3f ray, Frame3f handFrame, int nStep)
        {
            // scene xform may have changed during steps (eg view rotation), so we
            // need to reconstruct our local frame
            Frame3f primCurW = scene.ToWorldFrame(primStartS);

            // step 1: find radius in plane
            if (nStep == 0) {
                Vector3f forwardDir = ray.Direction;
                Vector3f plane_hit = VRUtil.SafeRayPlaneIntersection(ray, forwardDir, primCurW.Origin, primCurW.Y);
                plane_hit_local = primCurW.ToFrameP(plane_hit);
            }
            float fX = MathUtil.SignedClamp(plane_hit_local[0], MinDimension, MaxDimension);
            float fY = MinDimension;
            float fZ = MathUtil.SignedClamp(plane_hit_local[2], MinDimension, MaxDimension);
            float fR_plane = MathUtil.Clamp(plane_hit_local.Length, MinDimension / 2, MaxDimension / 2);

            // step 2: find height from plane
            if (nStep == 1) {
                Vector3f plane_hit = primCurW.FromFrameP(plane_hit_local);
                Line3d l = new Line3d(plane_hit, primCurW.Y);
                Vector3f handTip = handFrame.Origin + SceneGraphConfig.HandTipOffset * handFrame.Z;
                float fHandDist = (float)l.DistanceSquared(handTip);
                if (fHandDist < fR_plane * 1.5f) {
                    fY = (float)l.Project(handTip);
                } else {
                    fY = (float)DistLine3Ray3.MinDistanceLineParam(ray, l);
                }
            }

            // figure out possible dimensions, clamp to ranges
            fY = MathUtil.SignedClamp(fY, MinDimension, MaxDimension);

            // update frame
            primitive.Frame = primCurW;

            // update dimensions
            bool bIsCorner = (primitive.Center == CenterModes.Corner);
            float fScale = 1.0f;        // object is not in scene coordinates!
            if (primitive.Type == MeshPrimitivePreview.PrimType.Cylinder) {
                primitive.Width = (bIsCorner) ? fR_plane * fScale : 2 * fR_plane * fScale;
                primitive.Depth = primitive.Width;
                //primitive.Depth = Mathf.Sign(fZ) * primitive.Width;
                //primitive.Width = Mathf.Sign(fX) * primitive.Width;
                primitive.Height = fY * fScale;

            } else if (primitive.Type == MeshPrimitivePreview.PrimType.Box) {
                primitive.Width = (bIsCorner) ? fX : 2 * fX * fScale;
                primitive.Depth = (bIsCorner) ? fZ : 2 * fZ * fScale;
                primitive.Height = fY * fScale;

            } else if (primitive.Type == MeshPrimitivePreview.PrimType.Sphere) {
                primitive.Width = (bIsCorner) ? fR_plane * fScale : 2 * fR_plane * fScale;
                primitive.Depth = primitive.Height = primitive.Width;
                //primitive.Depth = Mathf.Sign(fZ) * primitive.Width;
                //primitive.Width = Mathf.Sign(fX) * primitive.Width;
                //primitive.Height = Mathf.Sign(fY) * primitive.Width;


            } else
                throw new NotImplementedException("DrawPrimitivesTool.UpdateDraw_Ray - type not supported");
        }



        public void EndDraw()
        {
            // store undo/redo record for new primitive
            PrimitiveSO primSO = primitive.BuildSO(scene, scene.DefaultSOMaterial);
            scene.History.PushChange(
                new AddSOChange() { scene = scene, so = primSO, bKeepWorldPosition = false });
            scene.History.PushInteractionCheckpoint();

            primitive.Destroy();
            primitive = null;
        }

        public void CancelDraw()
        {
            if (primitive != null) {
                primitive.Destroy();
                primitive = null;
            }
        }


    }







    class DrawPrimitivesTool_SpatialDeviceBehavior : StandardInputBehavior
    {
        FContext context;

        public DrawPrimitivesTool_SpatialDeviceBehavior(FContext s)
        {
            context = s;
        }

        override public InputDevice SupportedDevices
        {
            get { return InputDevice.AnySpatialDevice; }
        }

        class capture_data
        {
            public int nStep;
        }

        override public CaptureRequest WantsCapture(InputState input)
        {
            if (input.bLeftTriggerPressed ^ input.bRightTriggerPressed) {
                CaptureSide eSide = (input.bLeftTriggerPressed) ? CaptureSide.Left : CaptureSide.Right;
                Ray sideRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
                ITool tool = context.ToolManager.GetActiveTool((int)eSide);
                if (tool != null && tool is DrawPrimitivesTool) {
                    AnyRayHit rayHit;
                    if (context.Scene.FindSceneRayIntersection(sideRay, out rayHit))
                        return CaptureRequest.Begin(this, eSide);
                }
            }
            return CaptureRequest.Ignore;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            Ray sideRay = (eSide == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            Frame3f sideHandF = (eSide == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            DrawPrimitivesTool tool = context.ToolManager.GetActiveTool((int)eSide) as DrawPrimitivesTool;

            AnyRayHit rayHit;
            if (context.Scene.FindSceneRayIntersection(sideRay, out rayHit)) {
                tool.BeginDraw_Spatial(sideRay, rayHit, sideHandF, 0);
                return Capture.Begin(this, eSide, new capture_data() { nStep = 0 });
            }
            return Capture.Ignore;
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            DrawPrimitivesTool tool = context.ToolManager.GetActiveTool((int)data.which) as DrawPrimitivesTool;

            // [RMS] this is a hack for trigger+shoulder grab gesture...really need some way
            //   to interrupt captures!!
            if ( (data.which == CaptureSide.Left && input.bLeftShoulderPressed) ||
                 (data.which == CaptureSide.Right && input.bRightShoulderPressed) ) {
                tool.CancelDraw();
                return Capture.End;
            }

            Ray sideRay = (data.which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay;
            Frame3f sideHandF = (data.which == CaptureSide.Left) ? input.LeftHandFrame : input.RightHandFrame;
            capture_data cap = data.custom_data as capture_data;

            tool.UpdateDraw_Spatial(sideRay, sideHandF, cap.nStep);

            bool bReleased = (data.which == CaptureSide.Left) ? input.bLeftTriggerReleased : input.bRightTriggerReleased;
            if (bReleased) {
                if ((cap.nStep + 1) < tool.Steps) {
                    cap.nStep++;
                    return Capture.Continue;
                } else {
                    tool.EndDraw();
                    return Capture.End;
                }
            } else
                return Capture.Continue;
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            DrawPrimitivesTool tool = context.ToolManager.GetActiveTool((int)data.which) as DrawPrimitivesTool;
            tool.CancelDraw();
            return Capture.End;
        }


        public override bool EnableHover {
            get { return false; }
        }
        public override void UpdateHover(InputState input) {
        }
        public override void EndHover(InputState input) {
        }
    }








    class DrawPrimitivesTool_MouseBehavior : StandardInputBehavior
    {
        FContext context;

        public DrawPrimitivesTool_MouseBehavior(FContext s)
        {
            context = s;
        }

        override public InputDevice SupportedDevices {
            get { return InputDevice.Mouse; }
        }

        override public CaptureRequest WantsCapture(InputState input) {
            if ( context.ToolManager.ActiveRightTool == null || ! (context.ToolManager.ActiveRightTool is DrawPrimitivesTool) )
                return CaptureRequest.Ignore;
            if (input.bLeftMousePressed) {
                AnyRayHit rayHit;
                if (context.Scene.FindSceneRayIntersection(input.vMouseWorldRay, out rayHit))
                    return CaptureRequest.Begin(this);
            }
            return CaptureRequest.Ignore;
        }

        class capture_data
        {
            public int nStep;
        }

        override public Capture BeginCapture(InputState input, CaptureSide eSide)
        {
            DrawPrimitivesTool tool =
                (context.ToolManager.ActiveRightTool as DrawPrimitivesTool);
            AnyRayHit rayHit;
            if (context.Scene.FindSceneRayIntersection(input.vMouseWorldRay, out rayHit)) {
                tool.BeginDraw_Ray(input.vMouseWorldRay, rayHit, 0);
                return Capture.Begin(this, CaptureSide.Any, new capture_data() { nStep = 0 } );
            }
            return Capture.Ignore;
        }


        override public Capture UpdateCapture(InputState input, CaptureData data)
        {
            DrawPrimitivesTool tool =
                (context.ToolManager.ActiveRightTool as DrawPrimitivesTool);
            capture_data cap = data.custom_data as capture_data;

            tool.UpdateDraw_Ray(input.vMouseWorldRay, cap.nStep);

            if ( input.bLeftMouseReleased ) {
                if ( (cap.nStep+1) < tool.Steps) {
                    cap.nStep++;
                    return Capture.Continue;
                } else {
                    tool.EndDraw();
                    return Capture.End;
                }
            } else
                return Capture.Continue;
        }

        override public Capture ForceEndCapture(InputState input, CaptureData data)
        {
            DrawPrimitivesTool tool =
                (context.ToolManager.ActiveRightTool as DrawPrimitivesTool);
            tool.CancelDraw();
            return Capture.End;
        }


        public override bool EnableHover {
            get { return false; }
        }
        public override void UpdateHover(InputState input) {
        }
        public override void EndHover(InputState input)
        {
        }
    }


}
