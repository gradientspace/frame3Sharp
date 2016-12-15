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







        public static void ShowCenteredStaticPopupMessage(string sImagePath, Cockpit cockpit)
        {
            Material mat = MaterialUtil.CreateTransparentImageMaterial(sImagePath);
            float fAspect = (float)mat.mainTexture.width / (float)mat.mainTexture.height;
            float fScale = 0.5f;        // should this be a parameter??

            HUDPopupMessage message = new HUDPopupMessage() {
                Shape = new HUDShape() {
                    Type = HUDShapeType.Rectangle, Width = fScale*1.0f, Height = fScale * 1.0f / fAspect,
                    UseUVSubRegion = false
                }
            };
            message.Create(mat);
            HUDUtil.PlaceInSphere(message, 0.5f, 0, 0);
            message.Name = "popup";
            cockpit.AddUIElement(message, true);
            message.OnDismissed += (s, e) => {
                AnimatedDimiss_Cockpit(message, cockpit);
            };
            AnimatedShow(message);
        }


        public static void ShowToastStaticPopupMessage(string sImagePath, Cockpit cockpit)
        {
            Material mat = MaterialUtil.CreateTransparentImageMaterial(sImagePath);
            float fAspect = (float)mat.mainTexture.width / (float)mat.mainTexture.height;
            float fScale = 2.0f;        // should this be a parameter??

            HUDPopupMessage message = new HUDPopupMessage() {
                Shape = new HUDShape() {
                    Type = HUDShapeType.Rectangle, Width = fScale * 1.0f, Height = fScale * 1.0f / fAspect,
                    UseUVSubRegion = false
                }
            };
            message.Create(mat);
            HUDUtil.PlaceInSphere(message, 3.0f, 30, -10);
            UnityUtil.TranslateInFrame(message.RootGameObject, 0.75f, -0.5f, 0.0f, CoordSpace.WorldCoords);
            message.Name = "popup";
            cockpit.AddUIElement(message, true);
            message.OnDismissed += (s, e) => {
                AnimatedDimiss_Cockpit(message, cockpit);
            };
            AnimatedShow(message, 0.5f);
        }




        public static void AnimatedShow(HUDStandardItem hudItem, float fDuration = 0.25f)
        {
            AnimatedDisplayHUDItem anim = hudItem.RootGameObject.AddComponent<AnimatedDisplayHUDItem>();
            anim.Play(hudItem, fDuration);
        }

        public static void AnimatedDimiss_Cockpit(HUDStandardItem hudItem, Cockpit cockpit, float fDuration = 0.25f)
        {
            AnimatedDismissHUDItem anim = hudItem.RootGameObject.AddComponent<AnimatedDismissHUDItem>();
            anim.CompleteCallback += () => {
                anim.HUDItem.ClearGameObjects(false);
                cockpit.RemoveUIElement(anim.HUDItem, true);
            };
            anim.Play(hudItem, fDuration);
        }

        public static void AnimatedDimiss_Scene(HUDStandardItem hudItem, FScene scene, float fDuration = 0.25f)
        {
            AnimatedDismissHUDItem anim = hudItem.RootGameObject.AddComponent<AnimatedDismissHUDItem>();
            anim.CompleteCallback += () => {
                anim.HUDItem.ClearGameObjects(false);
                scene.RemoveUIElement(anim.HUDItem, true);
            };
            anim.Play(hudItem, fDuration);
        }


        public static bool FindNearestRayIntersection(List<SceneObject> vObjects, Ray ray, out SORayHit hit, Func<SceneObject,bool> filter = null)
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


        public static bool FindNearestRayIntersection(List<SceneUIElement> vElements, Ray ray, out UIRayHit hit)
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
        public static bool FindNearestHoverRayIntersection(List<SceneUIElement> vElements, Ray ray, out UIRayHit hit)
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

        public List<GameObject> Generate()
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

            return new List<GameObject>() { gameObj };
        }
    }



    // create text mesh
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

        public List<GameObject> Generate()
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

            return new List<GameObject>() { gameObj };
        }
    }

}
