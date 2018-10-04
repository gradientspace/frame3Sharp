using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    //
    // Set of 'indicator' elements that are attached to an SO, and hence move with it.
    // These are meant to be transient, eg like something shown during an interaction.
    // 
    // The indicator GameObjects collected under a parent GO, which is itself made a child
    //  of the parent SO. Frames passed in to specify location of indicators should be
    //  in the cordinate space of this parent SO, ***including any local scaling***
    //    (if not, can compensate in add functions with bIsScaled flag)
    //
    // Assumption is that indicators should maintain fixed size during view changes,
    // this is done in PreRender(). To make sure that PreRender() is called, a
    // PreRenderHelper SceneUIElement is added to scene on constructor and removed
    // on Disconnect (which you should call!!)
    //
    public class SOIndicatorSet
    {
        SceneObject parentSO;

        GameObject parentGO;
        PreRenderHelper helper;

        struct IndicatorInfo
        {
            public GameObject go;
            public float fVisualRadiusDeg;
        }
        List<IndicatorInfo> vObjects;

        public SOIndicatorSet(SceneObject so)
        {
            parentSO = so;
            vObjects = new List<IndicatorInfo>();

            Initialize();
        }


        public void Initialize()
        {
            parentGO = new GameObject(parentSO.Name + "_indicators");
            UnityUtil.AddChild(parentSO.RootGameObject, parentGO, false);

            helper = new PreRenderHelper("indicators_helper") {
                PreRenderF = () => { this.PreRender(parentSO.GetScene().ActiveCamera.GetPosition()); }
            };
            parentSO.GetScene().AddUIElement(helper);
        }

        public void Disconnect(bool bDestroy)
        {
            parentGO.transform.parent = null;
            if (bDestroy) {
                parentGO.Destroy();
                if (helper != null)
                    parentSO.GetScene().RemoveUIElement(helper, true);
            } else {
                if (helper != null)
                    parentSO.GetScene().RemoveUIElement(helper, false);
            }
        }


        public void PreRender(Vector3f cameraPosW)
        {
            float fSceneScale = parentSO.GetScene().GetSceneScale();
            foreach (var i in vObjects) {
                Vector3f vPos = i.go.transform.position;
                float fR = VRUtil.GetVRRadiusForVisualAngle(vPos, cameraPosW, i.fVisualRadiusDeg);

                // have to compensate for scaling of parent SO
                float fParentSOScale = parentSO.GetLocalScale()[0];
                fR = fR / fParentSOScale / fSceneScale;

                i.go.transform.localScale = fR * Vector3f.One;
            }
        }


        // pass bIsScaled=false if the localFrame is not in the un-scaled space of the primitive
        //   (eg like if you computed positions relative to an un-scaled bounding box)
        public void AddSphereL(Frame3f localFrame, float fVisualRadiusDeg, Material mat, bool bIsScaled = true )
        {
            GameObject go = new GameObject("sphere");
            Vector3f vScale = parentSO.GetLocalScale();

            go.AddComponent<MeshFilter>();
            go.SetMesh(UnityUtil.GetPrimitiveMesh(PrimitiveType.Sphere));
            var goRen = go.AddComponent<MeshRenderer>();
            goRen.material = mat;
            go.transform.localScale = new Vector3(1,1,1);
            // we are going to be parented to SO, so we will inherit its local scale.
            // assumption is that frames passed in are already in scaled coordinates, 
            // so we need to undo that scale
            if (bIsScaled)
                go.transform.position = localFrame.Origin / vScale;
            else
                go.transform.position = localFrame.Origin;
            go.layer = FPlatform.WidgetOverlayLayer;

            UnityUtil.AddChild(parentGO, go, false);

            vObjects.Add(new IndicatorInfo() { go = go, fVisualRadiusDeg = fVisualRadiusDeg });
        }

    }







    public class SOSceneIndicatorSet : IndicatorSet
    {
        SceneObject parentSO;
        fGameObject sceneParentGO;

        public SOSceneIndicatorSet(SceneObject parentSO, FScene scene) : base(scene)
        {
            this.parentSO = parentSO;
            Initialize();
        }

        public virtual void Initialize()
        {
            sceneParentGO = GameObjectFactory.CreateParentGO(parentSO.Name + "_indicators_scene");
            UnityUtil.AddChild(Scene.TransientObjectsParent, sceneParentGO, false);
        }

        public override void AddIndicator(Indicator i)
        {
            base.AddIndicator(i);
            CoordSpace eSpace = i.InSpace;
            if (eSpace == CoordSpace.ObjectCoords || eSpace == CoordSpace.WorldCoords)
                throw new Exception("SOSceneIndicatorSet.AddIndicator: only scene-space indicators not supported!");

            UnityUtil.AddChild(sceneParentGO, i.RootGameObject, false);
        }


        public override void Disconnect(bool bDestroy)
        {
            sceneParentGO.SetParent(null);
            base.Disconnect(bDestroy);
            if (bDestroy) 
                sceneParentGO.Destroy();
        }

    }


}
