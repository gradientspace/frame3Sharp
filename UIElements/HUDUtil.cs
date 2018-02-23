using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    // [RMS] this is partial because we have some things to add in platform componets
    public partial class HUDUtil
    {
        // returns frame at ray-intersection point, with normal pointing *outwards*
        public static Frame3f GetSphereFrame(float fHUDRadius, float fHorzAngleDeg, float fVertAngleDeg)
        {
            Ray3f r = VRUtil.MakeRayFromSphereCenter(fHorzAngleDeg, fVertAngleDeg);
            float fRayT = 0.0f;
            RayIntersection.Sphere(r.Origin, r.Direction, Vector3f.Zero, fHUDRadius, out fRayT);
            Vector3f v = r.Origin + fRayT * r.Direction;
            return new Frame3f(v, v.Normalized);
        }


        public static Frame3f GetSphereFrame(float fHUDRadius, Vector3f vHUDCenter, Vector3f vPosition, bool bInwards = false)
        {
            Vector3f n = (vPosition - vHUDCenter).Normalized;
            Vector3f p = vHUDCenter + fHUDRadius * n;
            return new Frame3f(p, (bInwards) ? -n : n);
        }


        public static void PlaceInSphere(HUDStandardItem hudItem, float fHUDRadius, float fAngleHorz, float fAngleVert)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = GetSphereFrame(fHUDRadius, fAngleHorz, fAngleVert);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternionf.FromTo(initFrame.Z, hudFrame.Z)));
        }

        public static void PlaceInSphereWithNormal(HUDStandardItem hudItem, float fHUDRadius, float fAngleHorz, float fAngleVert, Vector3f vPointDir)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = GetSphereFrame(fHUDRadius, fAngleHorz, fAngleVert);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternionf.FromTo(initFrame.Z, vPointDir)));
        }


        public static void PlaceInSphere(HUDStandardItem hudItem, float fHUDRadius, Vector3f vHUDCenter, Vector3f vPlaceAt)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Frame3f hudFrame = GetSphereFrame(fHUDRadius, vHUDCenter, vPlaceAt);
            hudItem.SetObjectFrame(
                initFrame.Translated(hudFrame.Origin)
                .Rotated(Quaternionf.FromTo(initFrame.Z, hudFrame.Z)));
        }


        public static void PlaceInScene(HUDStandardItem hudItem, Vector3f vHUDCenter, Vector3f vPlaceAt)
        {
            Frame3f initFrame = hudItem.GetObjectFrame();
            Vector3f n = (vPlaceAt - vHUDCenter).Normalized;
            Frame3f frame = new Frame3f(vPlaceAt, n);
            hudItem.SetObjectFrame(
                initFrame.Translated(frame.Origin)
                .Rotated(Quaternionf.FromTo(initFrame.Z, frame.Z)));
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
            Ray3f r = VRUtil.MakeRayFromSphereCenter(fHorzAngleDeg, fVertAngleDeg);
            float fRayT = 0.0f;
            RayIntersection.InfiniteCylinder(r.Origin, r.Direction, Vector3f.Zero, Vector3f.AxisY, fHUDRadius, out fRayT);
            Vector3f v = r.Origin + fRayT * r.Direction;
            Vector3f n = new Vector3f(v[0], 0, v[2]).Normalized;
            return new Frame3f(v, n);
        }


        public static Frame3f GetCylinderFrameFromAngleHeight(float fHUDRadius, float fHorzAngleDeg, float fVertHeight)
        {
            Ray3f r = VRUtil.MakeRayFromSphereCenter(fHorzAngleDeg, 0);
            r.Direction = fHUDRadius * r.Direction + fVertHeight * Vector3f.AxisY;
            r.Direction.Normalize();
            float fRayT = 0.0f;
            RayIntersection.InfiniteCylinder(r.Origin, r.Direction, Vector3f.Zero, Vector3f.AxisY, fHUDRadius, out fRayT);
            Vector3f v = r.Origin + fRayT * r.Direction;
            Vector3f n = new Vector3f(v[0], 0, v[2]).Normalized;
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


        public static bool FindNearestRayIntersection(IEnumerable<SceneObject> vObjects, Ray3f ray, out SORayHit hit, Func<SceneObject,bool> filter = null)
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


        public static bool FindNearestRayIntersection(IEnumerable<SceneUIElement> vElements, Ray3f ray, out UIRayHit hit)
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
        public static bool FindNearestHoverRayIntersection(IEnumerable<SceneUIElement> vElements, Ray3f ray, out UIRayHit hit)
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
        public Vector3f Translate { get; set; }
        public Quaternionf Rotate { get; set; }
        public Colorf Color { get; set; }

        public IconMeshGenerator()
        {
            Scale = 0.1f;
            Translate = new Vector3f(0, 0, 0);
            Rotate = Quaternionf.AxisAngleD(Vector3f.AxisY, 120.0f);
            Color = Colorf.Grey;
        }

        public List<fGameObject> Generate()
        {
            fMesh mesh = FResources.LoadMesh(Path);
            fMeshGameObject fMeshGO = GameObjectFactory.CreateMeshGO("iconMesh", mesh, false, true);
            fMeshGO.SetMaterial(MaterialUtil.CreateStandardMaterial(this.Color), true);

            // apply orientation
            fMeshGO.SetLocalScale(new Vector3f(Scale, Scale, Scale));
            fMeshGO.SetLocalPosition(fMeshGO.GetLocalPosition() + Translate);
            fMeshGO.SetLocalRotation(Rotate);

            // ignore material changes when we add to GameObjectSet
            fMeshGO.AddComponent<IgnoreMaterialChanges>();

            return new List<fGameObject>() { fMeshGO };
        }
    }




}
