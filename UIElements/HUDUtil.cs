using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{


    public class HUDUtil
    {
        // returns frame at ray-intersection point, with normal pointing *outwards*
        public static Frame3f GetSphereFrame(float fHUDRadius, float fHorzAngleDeg, float fVertAngleDeg)
        {
            Ray r = VRUtil.MakeRayFromSphereCenter(fHorzAngleDeg, fVertAngleDeg);
            float fRayT = 0.0f;
            RayIntersection.Sphere(r.origin, r.direction, Vector3.zero, fHUDRadius, out fRayT);
            Vector3 v = r.origin + fRayT * r.direction;
            return new Frame3f(v, v.normalized);
        }


        public static Frame3f GetSphereFrame(float fHUDRadius, Vector3 vHUDCenter, Vector3 vPosition, bool bInwards = false)
        {
            Vector3 n = (vPosition - vHUDCenter).normalized;
            Vector3 p = vHUDCenter + fHUDRadius * n;
            return new Frame3f(p, (bInwards) ? -n : n);
        }


        public static void PlaceInSphere(HUDStandardItem hudItem, float fHUDRadius, float fAngleHorz, float fAngleVert)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = GetSphereFrame(fHUDRadius, fAngleHorz, fAngleVert);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternion.FromToRotation(initFrame.Z, hudFrame.Z)));
        }

        public static void PlaceInSphereWithNormal(HUDStandardItem hudItem, float fHUDRadius, float fAngleHorz, float fAngleVert, Vector3 vPointDir)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = GetSphereFrame(fHUDRadius, fAngleHorz, fAngleVert);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternion.FromToRotation(initFrame.Z, vPointDir)));
        }


        public static void PlaceInSphere(HUDStandardItem hudItem, float fHUDRadius, Vector3 vHUDCenter, Vector3 vPlaceAt)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = GetSphereFrame(fHUDRadius, vHUDCenter, vPlaceAt);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternion.FromToRotation(initFrame.Z, hudFrame.Z)));
        }


        public static void PlaceInScene(HUDStandardItem hudItem, Vector3 vHUDCenter, Vector3 vPlaceAt)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Vector3 n = (vPlaceAt - vHUDCenter).normalized;
            Frame3f frame = new Frame3f(vPlaceAt, n);
            hudItem.SetObjectFrame(
                initFrame.Translated(frame.Origin)
                .Rotated(Quaternion.FromToRotation(initFrame.Z, frame.Z)));
        }


        public static void PlaceInViewPlane(HUDStandardItem hudItem, Vector3f vPosition, Frame3f viewFrame)
        {
            Frame3f objFrame = hudItem.GetObjectFrame().Translated(vPosition);
            hudItem.SetObjectFrame(viewFrame.FromFrame(objFrame));
        }
        public static void PlaceInViewPlane(HUDStandardItem hudItem, Frame3f viewFrame) {
            PlaceInViewPlane(hudItem, Vector3f.Zero, viewFrame);
        }




        // returns frame at ray-intersection point, with normal pointing *outwards*
        public static Frame3f GetCylinderFrameFromAngles(float fHUDRadius, float fHorzAngleDeg, float fVertAngleDeg)
        {
            Ray r = VRUtil.MakeRayFromSphereCenter(fHorzAngleDeg, fVertAngleDeg);
            float fRayT = 0.0f;
            RayIntersection.InfiniteCylinder(r.origin, r.direction, Vector3f.Zero, Vector3f.AxisY, fHUDRadius, out fRayT);
            Vector3 v = r.origin + fRayT * r.direction;
            Vector3 n = new Vector3(v[0], 0, v[2]).normalized;
            return new Frame3f(v, n);
        }


        public static Frame3f GetCylinderFrameFromAngleHeight(float fHUDRadius, float fHorzAngleDeg, float fVertHeight)
        {
            Ray r = VRUtil.MakeRayFromSphereCenter(fHorzAngleDeg, 0);
            r.direction = fHUDRadius * r.direction + fVertHeight * Vector3.up;
            r.direction.Normalize();
            float fRayT = 0.0f;
            RayIntersection.InfiniteCylinder(r.origin, r.direction, Vector3f.Zero, Vector3f.AxisY, fHUDRadius, out fRayT);
            Vector3 v = r.origin + fRayT * r.direction;
            Vector3 n = new Vector3(v[0], 0, v[2]).normalized;
            return new Frame3f(v, n);
        }



        /// <summary>
        /// Create a fMesh of the shape/dimensions specified by Shape
        /// </summary>
        public static fMesh MakeBackgroundMesh(HUDShape Shape)
        {
            if (Shape.Type == HUDShapeType.Disc) {
                return MeshGenerators.CreateTrivialDisc(Shape.Radius, Shape.Slices);
            } else if (Shape.Type == HUDShapeType.Rectangle) {
                return MeshGenerators.CreateTrivialRect(Shape.Width, Shape.Height,
                    Shape.UseUVSubRegion == true ? 
                        MeshGenerators.UVRegionType.CenteredUVRectangle : MeshGenerators.UVRegionType.FullUVSquare);
            } else if (Shape.Type == HUDShapeType.RoundRect) {
                return MeshGenerators.CreateRoundRect(Shape.Width, Shape.Height, Shape.Radius, Shape.Slices,
                    Shape.UseUVSubRegion == true ?
                        MeshGenerators.UVRegionType.CenteredUVRectangle : MeshGenerators.UVRegionType.FullUVSquare,
                    Shape.RoundRectSharpCorners );
            } else {
                throw new Exception("HUDUtil.MakeBackgroundMesh: unknown shape type!");
            }
        }



        /// <summary>
        /// Replace the material on the background GO of item.
        /// Assumes that the background GO has the name "background"...
        /// </summary>
        public static void SetBackgroundMaterial(HUDStandardItem item, fMaterial material, bool bShared = false)
        {
            var go = item.FindGOByName("background");
            if (go == null) {
                DebugUtil.Log(2, "HUDUtil.SetBackgroundImage: item {0} does not have go named 'background'", item.Name);
                return;
            }

            go.SetMaterial(material, bShared);
        }



        /// <summary>
        /// This is very hacky.
        /// </summary>
        public static void AddDropShadow(HUDStandardItem item, Cockpit cockpit, Colorf color, 
            float falloffWidthPx, Vector2f offset, float fZShift, bool bTrackCockpitScaling = true)
        {
            if (item is IBoxModelElement == false)
                throw new Exception("HUDUtil.AddDropShadow: can only add drop shadow to IBoxModelElement");

            float falloffWidth = falloffWidthPx * cockpit.GetPixelScale();

            // [TODO] need interface that provides a HUDShape?
            var shape = item as IBoxModelElement;
            float w = shape.Size2D.x + falloffWidth;
            float h = shape.Size2D.y + falloffWidth;

            fRectangleGameObject meshGO = GameObjectFactory.CreateRectangleGO("shadow", w, h, color, false);
            meshGO.RotateD(Vector3f.AxisX, -90.0f);
            fMaterial dropMat = MaterialUtil.CreateDropShadowMaterial(color, w, h, falloffWidth);
            meshGO.SetMaterial(dropMat);

            item.AppendNewGO(meshGO, item.RootGameObject, false);
            BoxModel.Translate(meshGO, offset, fZShift);

            if (bTrackCockpitScaling) {
                PreRenderBehavior pb = meshGO.AddComponent<PreRenderBehavior>();
                pb.ParentFGO = meshGO;
                pb.AddAction(() => {
                    Vector3 posW = item.RootGameObject.PointToWorld(meshGO.GetLocalPosition());
                    ((Material)dropMat).SetVector("_Center", new Vector4(posW.x, posW.y, posW.z, 0));
                    float curWidth = falloffWidthPx * cockpit.GetPixelScale();
                    Vector2f origSize = shape.Size2D + falloffWidth * Vector2f.One;
                    Vector2f size = cockpit.GetScaledDimensions(origSize);
                    float ww = size.x;
                    float hh = size.y;
                    ((Material)dropMat).SetVector("_Extents", new Vector4(ww / 2, hh / 2, 0, 0));
                    float newWidth = falloffWidthPx * cockpit.GetPixelScale();
                    ((Material)dropMat).SetFloat("_FalloffWidth", newWidth);
                });
            }
        }




        public static void ShowCenteredPopupMessage(string sTitleText, string sText, Cockpit cockpit)
        {
            HUDPopupMessage message = new HUDPopupMessage() {
                Width = 500*cockpit.GetPixelScale(), Height = 250*cockpit.GetPixelScale(),
                TitleTextHeight = 30 * cockpit.GetPixelScale(),
                TextHeight = 20 * cockpit.GetPixelScale(),
                BackgroundColor = Colorf.Silver,
                TextColor = Colorf.VideoBlack,
                TitleText = sTitleText,
                Text = sText
            };
            message.Create();
            if ( FPlatform.IsUsingVR() )
                HUDUtil.PlaceInSphere(message, 0.5f, 0, 0);
            else
                HUDUtil.PlaceInSphere(message, 1.5f, 0, 0);
            message.Name = "popup";
            cockpit.AddUIElement(message, true);
            message.OnDismissed += (s, e) => {
                AnimatedDimiss_Cockpit(message, cockpit, true);
            };
            AnimatedShow(message);
        }


        public static void ShowToastPopupMessage(string sText, Cockpit cockpit, float heightScale = 1.0f, float textScale = 1.0f)
        {
            // [TODO] should size based on VR or not-VR...for VR use visual radius?

            HUDPopupMessage message = new HUDPopupMessage() {
                Width = 500*cockpit.GetPixelScale(), Height = heightScale*150*cockpit.GetPixelScale(),
                TextHeight = textScale * 50 * cockpit.GetPixelScale(),
                BackgroundColor = Colorf.DarkYellow,
                TextColor = Colorf.VideoBlack,
                Text = sText
            };
            message.Create();
            HUDUtil.PlaceInSphere(message, 1.0f, 30, -30);
            message.Name = "popup";
            cockpit.AddUIElement(message, true);
            message.OnDismissed += (s, e) => {
                AnimatedDimiss_Cockpit(message, cockpit, true);
            };
            AnimatedShow(message, 0.5f);
        }




        public static void AnimatedShow(HUDStandardItem hudItem, float fDuration = 0.25f, Action OnCompleted = null)
        {
            AnimatedDisplayHUDItem anim = hudItem.RootGameObject.AddComponent<AnimatedDisplayHUDItem>();
            if (OnCompleted != null) {
                anim.CompleteCallback += () => {
                    OnCompleted();
                };
            }
            anim.Play(hudItem, fDuration);
        }

        public static void AnimatedDimiss_Cockpit(HUDStandardItem hudItem, Cockpit cockpit, bool bDestroy, float fDuration = 0.25f)
        {
            AnimatedDismissHUDItem anim = hudItem.RootGameObject.AddComponent<AnimatedDismissHUDItem>();
            anim.CompleteCallback += () => {
                if ( bDestroy )
                    anim.HUDItem.ClearGameObjects(false);       // what is this line for??
                cockpit.RemoveUIElement(anim.HUDItem, bDestroy);
                if (bDestroy == false)
                    anim.HUDItem.IsVisible = false;
            };
            anim.Play(hudItem, fDuration);
        }


        public static void AnimatedShowHide_Cockpit(HUDStandardItem hudItem, Cockpit cockpit, float fShowDuration = 3.0f, float fFadeDuration = 0.25f)
        {
            AnimatedShowHideHUDItem anim = hudItem.RootGameObject.AddComponent<AnimatedShowHideHUDItem>();
            anim.CompleteCallback += () => {
                anim.HUDItem.ClearGameObjects(false);
                cockpit.RemoveUIElement(anim.HUDItem, true);
            };
            anim.Play(hudItem, fShowDuration, fFadeDuration);
        }

        public static void AnimatedDimiss_Scene(HUDStandardItem hudItem, FScene scene, bool bDestroy, float fDuration = 0.25f)
        {
            AnimatedDismissHUDItem anim = hudItem.RootGameObject.AddComponent<AnimatedDismissHUDItem>();
            anim.CompleteCallback += () => {
                anim.HUDItem.ClearGameObjects(false);
                scene.RemoveUIElement(anim.HUDItem, bDestroy);
            };
            anim.Play(hudItem, fDuration);
        }


        public static bool FindNearestRayIntersection(IEnumerable<SceneObject> vObjects, Ray ray, out SORayHit hit, Func<SceneObject,bool> filter = null)
        {
            hit = null;

            foreach (var so in vObjects) {
                if (filter != null && filter(so) == false)
                    continue;
                SORayHit soHit;
                if (so.FindRayIntersection(ray, out soHit)) {
                    if (hit == null || soHit.fHitDist < hit.fHitDist)
                        hit = soHit;
                }
            }
            return (hit != null);
        }


        public static bool FindNearestRayIntersection(IEnumerable<SceneUIElement> vElements, Ray ray, out UIRayHit hit)
        {
            hit = null;

            foreach (var ui in vElements) {
                UIRayHit uiHit;
                if (ui.FindRayIntersection(ray, out uiHit)) {
                    if (hit == null || uiHit.fHitDist < hit.fHitDist)
                        hit = uiHit;
                }
            }
            return (hit != null);
        }
        public static bool FindNearestHoverRayIntersection(IEnumerable<SceneUIElement> vElements, Ray ray, out UIRayHit hit)
        {
            hit = null;

            foreach (var ui in vElements) {
                UIRayHit uiHit;
                if (ui.EnableHover && ui.FindHoverRayIntersection(ray, out uiHit)) {
                    if (hit == null || uiHit.fHitDist < hit.fHitDist)
                        hit = uiHit;
                }
            }
            return (hit != null);
        }


    }



    // create an icon-style 3D mesh
    // [TODO] not used? perhaps in VRCAD....?
    public class IconMeshGenerator : IGameObjectGenerator
    {
        public string Path { get; set; }
        public float Scale { get; set; }
        public Vector3 Translate { get; set; }
        public Quaternion Rotate { get; set; }
        public Color Color { get; set; }

        public IconMeshGenerator()
        {
            Scale = 0.1f;
            Translate = new Vector3(0, 0, 0);
            Rotate = Quaternion.AngleAxis(120.0f, Vector3.up);
            Color = Color.grey;
        }

        public List<fGameObject> Generate()
        {
            var gameObj = new GameObject("iconMesh");
            var gameObjMesh = (MeshFilter)gameObj.AddComponent(typeof(MeshFilter));
            gameObj.SetMesh((Mesh)Resources.Load(Path, typeof(Mesh)));
            MeshRenderer ren = gameObj.AddComponent<MeshRenderer>();
            ren.material = MaterialUtil.CreateStandardMaterial(this.Color);

            // apply orientation
            gameObjMesh.transform.localScale = new Vector3(Scale, Scale, Scale);
            gameObjMesh.transform.localPosition += Translate;
            gameObjMesh.transform.localRotation = Rotate;

            // ignore material changes when we add to GameObjectSet
            gameObj.AddComponent<IgnoreMaterialChanges>();

            return new List<fGameObject>() { gameObj };
        }
    }



    // create text mesh
    // [TODO] only used by HUDRadialMenu, can get rid of when
    //   we replace that with fText...
    public class TextLabelGenerator : IGameObjectGenerator
    {
        public string Text { get; set; }
        public float Scale { get; set; }
        public Vector3 Translate { get; set; }
        public Color Color { get; set; }
        public float ZOffset { get; set; }

        public TextAlignment TextAlign { get; set; }

        public enum Alignment
        {
            Default, HCenter, VCenter, HVCenter
        }
        public Alignment Align { get; set; }

        public TextLabelGenerator()
        {
            Text = "(label)";
            TextAlign = TextAlignment.Center;
            Scale = 0.1f;
            Translate = new Vector3(0, 0, 0);
            Color = ColorUtil.make(10,10,10);
            ZOffset = -1.0f;
        }

        public List<fGameObject> Generate()
        {
            var gameObj = new GameObject("label");

            TextMesh tm = gameObj.AddComponent<TextMesh>();
            tm.text = Text;
            tm.color = Color;
            tm.fontSize = 50;
            tm.offsetZ = ZOffset;
            tm.alignment = TextAlign;

            // [RMS] this isn't quite right, on the vertical centering...
            Vector2 size = UnityUtil.EstimateTextMeshDimensions(tm);
            Vector3 vCenterShift = Vector3.zero;
            if (Align == Alignment.HCenter || Align == Alignment.HVCenter)
                vCenterShift.x -= size.x * 0.5f;
            if (Align == Alignment.VCenter || Align == Alignment.HVCenter)
                vCenterShift.y += size.y * 0.5f;

            // apply orientation
            float useScale = Scale;
            tm.transform.localScale = new Vector3(useScale, useScale, useScale);
            tm.transform.localPosition += useScale*vCenterShift;
            tm.transform.localPosition += Translate;

            // ignore material changes when we add to GameObjectSet
            gameObj.AddComponent<IgnoreMaterialChanges>();

            // use our textmesh material instead
            // [TODO] can we share between texts?
            MaterialUtil.SetTextMeshDefaultMaterial(tm);

            return new List<fGameObject>() { gameObj };
        }
    }

}
