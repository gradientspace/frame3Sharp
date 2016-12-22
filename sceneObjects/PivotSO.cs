using System;
using UnityEngine;

namespace f3
{
    public class PivotSO : BaseSO
    {
        GameObject pivot;
        GameObject meshGO;
        GameObject frameMesh;

        public PivotSO()
        {
            
        }

        public PivotSO Create(SOMaterial sphereMaterial, Material frameMaterial, int nSphereLayer = -1)
        {
            // [TODO] replace frame geometry with line renderer ?
            // [TODO] still cast shadows  (semitransparent objects don't cast shadows, apparently)
            // [TODO] maybe render solid when not obscured by objects? use raycast in PreRender?

            AssignSOMaterial(sphereMaterial);       // need to do this to setup BaseSO material stack

            pivot = new GameObject(UniqueNames.GetNext("Pivot"));
            meshGO = AppendUnityPrimitiveGO("pivotMesh", PrimitiveType.Sphere, CurrentMaterial, pivot);
            meshGO.transform.localScale = 0.9f * Vector3.one;
            frameMesh = UnityUtil.CreateMeshGO("pivotFrame", "icon_meshes/axis_frame", 1.0f,
                UnityUtil.MeshAlignOption.NoAlignment, frameMaterial, false);
            frameMesh.AddComponent<IgnoreMaterialChanges>();
            MaterialUtil.DisableShadows(frameMesh);
            AppendNewGO(frameMesh, pivot, false);

            if (nSphereLayer >= 0)
                meshGO.SetLayer(nSphereLayer);

            increment_timestamp();
            return this;
        }




        //
        // SceneObject impl
        //

        override public GameObject RootGameObject
        {
            get { return pivot; }
        }

        override public string Name
        {
            get { return pivot.GetName(); }
            set { pivot.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.Pivot; } }

        public override bool SupportsScaling
        {
            get { return false; }
        }

        public override void PreRender()
        {
            float fScaling = VRUtil.GetVRRadiusForVisualAngle(
                pivot.transform.position, 
                parentScene.ActiveCamera.GetPosition(), 
                SceneGraphConfig.DefaultPivotVisualDegrees );
            fScaling /= parentScene.GetSceneScale();
            pivot.transform.localScale = new Vector3(fScaling, fScaling, fScaling);
        }



        override public SceneObject Duplicate()
        {
            PivotSO copy = new PivotSO();
            copy.parentScene = this.parentScene;
            copy.Create(this.GetAssignedSOMaterial(), frameMesh.GetComponent<Renderer>().material, meshGO.layer);
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            return copy;
        }

    }

}
