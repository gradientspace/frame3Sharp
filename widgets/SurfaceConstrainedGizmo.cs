using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g3;

namespace f3
{

    public class SurfaceConstrainedGizmoBuilder : ITransformGizmoBuilder
    {
        public bool SupportsMultipleObjects { get { return false; } }

        public Vector3f WidgetScale = Vector3f.One;

        public ITransformGizmo Build(FScene scene, List<SceneObject> targets)
        {
            SurfaceConstrainedGizmo gizmo = gizmo_factory();
            gizmo.WidgetScale = WidgetScale;
            gizmo.Create(scene, targets);
            return gizmo;
        }

        protected virtual SurfaceConstrainedGizmo gizmo_factory()
        {
            return new SurfaceConstrainedGizmo();
        }
    }



    public class SurfaceConstrainedGizmo : BaseTransformGizmo
    {
        Material srcMaterial, srcHoverMaterial;
        List<Action> WidgetParameterUpdates;

        public List<SceneObject> ConstraintSurfaces;
        public Vector3f WidgetScale = Vector3f.One;

        /// <summary>
        /// element of ConstraintSurfaces that we hit on last interaction
        /// </summary>
        public SceneObject CurrentConstraintSurface = null;

        fMeshGameObject centerGO;
        fMeshGameObject rotateGO;

        public SurfaceConstrainedGizmo() : base()
        {
            ConstraintSurfaces = new List<SceneObject>();
            WidgetParameterUpdates = new List<Action>();
        }

        override public void Disconnect()
        {
            base.Disconnect();
            foreach (var target in Targets) {
                target.OnTransformModified -= on_transform_modified;
            }
        }


        // called on per-frame Update()
        override public void PreRender()
        {
            gizmo.Show();

            foreach (var v in Widgets) {
                float fScaling = VRUtil.GetVRRadiusForVisualAngle(
                   v.Key.GetPosition(),
                   parentScene.ActiveCamera.GetPosition(),
                   SceneGraphConfig.DefaultPivotVisualDegrees);
                fScaling /= parentScene.GetSceneScale();
                v.Key.SetLocalScale(fScaling * WidgetScale);
            }
        }


        virtual protected void make_materials()
        {
            float fAlpha = 0.5f;
            srcMaterial = MaterialUtil.CreateTransparentMaterial(ColorUtil.CgRed, fAlpha);
            srcHoverMaterial = MaterialUtil.CreateStandardMaterial(ColorUtil.CgRed);
        }


        override protected void BuildGizmo()
        {
            gizmo.SetName("SurfaceConstrainedGizmo");

            make_materials();

            centerGO = AppendMeshGO("object_origin",
                UnityUtil.GetPrimitiveMesh(PrimitiveType.Sphere), srcMaterial, gizmo);
            centerGO.SetLocalScale(WidgetScale);

            Widgets[centerGO] = new SurfaceConstrainedPointWidget(this, this.parentScene) {
                RootGameObject = centerGO, StandardMaterial = srcMaterial, HoverMaterial = srcHoverMaterial
            };


            PuncturedDiscGenerator discgen = new PuncturedDiscGenerator() {
                StartAngleDeg = 180, EndAngleDeg = 270, OuterRadius = 1.5f, InnerRadius = 0.75f
            };
            discgen.Generate();
            SimpleMesh discmesh = discgen.MakeSimpleMesh();
            MeshTransforms.Rotate(discmesh, Vector3d.Zero, Quaternionf.AxisAngleD(Vector3f.AxisX, 90));
            rotateGO = AppendMeshGO("object_rotate", new fMesh(discmesh), srcMaterial, gizmo);
            rotateGO.SetLocalScale(WidgetScale);

            Widgets[rotateGO] = new AxisRotationWidget(2) {
                RootGameObject = rotateGO, StandardMaterial = srcMaterial, HoverMaterial = srcHoverMaterial
            };                


            gizmo.Hide();
        }

        override protected void OnBeginCapture(Ray3f worldRay, Standard3DWidget w)
        {
            List<SceneObject> PotentialTargets = new List<SceneObject>(ConstraintSurfaces);
            foreach (var v in Widgets) {
                if (v.Value is SurfaceConstrainedPointWidget) {
                    SurfaceConstrainedPointWidget widget = v.Value as SurfaceConstrainedPointWidget;
                    widget.SourceSO = Targets[0];
                    widget.ConstraintSurfaces = PotentialTargets;
                }
            }
        }


        override protected void OnUpdateCapture(Ray3f worldRay, Standard3DWidget w)
        {
            if ( activeWidget is SurfaceConstrainedPointWidget ) {
                CurrentConstraintSurface = (activeWidget as SurfaceConstrainedPointWidget).CurrentConstraintSO;
            }
        }

        protected override void OnEndCapture(Ray3f worldRay, Standard3DWidget w)
        {
            //clear TargetObjects in widgets?
        }


        void on_transform_modified(SceneObject so)
        {
            foreach (var f in WidgetParameterUpdates)
                f();
        }


        //
        // ITransformGizmo impl
        //
        override public bool SupportsFrameMode { get { return false; } }

    }
}
