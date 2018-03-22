using System;
using g3;

namespace f3
{
    public class PivotSO : BaseSO, SpatialQueryableSO
    {
        fGameObject pivotGO;
        fGameObject shapeGO;
        fGameObject frameGO;

        SOMaterial frameMaterial;

        /// <summary>
        /// set fixed size of this PivotSO. Changing the Size results in
        /// scaling of the pivot in PreRender().
        /// </summary>
        public float Size {
            get { return size; }
            set { size = value; }
        }
        protected float creation_size = 0.0f;
        protected float size = 0.9f;

        /// <summary>
        /// A standard PivotSO is a "special" scene object, IE it is used more as a UI element.
        /// In that case we by default try to keep it the same "size" on-screen.
        /// Set to false to disable this behavior.
        /// </summary>
        public bool MaintainConsistentViewSize = true;


        /// <summary>
        /// Generally we use PivotSO as persistent objects in the scene, which are drawn "on top" of
        /// other SOs, and hence clicking on one should select it before overlapping SO is selected.
        /// If you set this to false, then FScene.FindSORayIntersection_PivotPriority() will not
        /// prioritize selection of this PivotSO (eg in case where you are using a PivotSO to 
        /// implement some other custom in-scene object type)
        /// </summary>
        public bool IsOverlaySO = true;


        public PivotSO()
        {
            
        }

        public virtual PivotSO Create(SOMaterial shapeMaterial, SOMaterial frameMaterial = null, int nShapeLayer = -1)
        {
            // [TODO] replace frame geometry with line renderer ?
            // [TODO] still cast shadows  (semitransparent objects don't cast shadows, apparently)
            // [TODO] maybe render solid when not obscured by objects? use raycast in PreRender?

            AssignSOMaterial(shapeMaterial);       // need to do this to setup BaseSO material stack

            pivotGO = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("Pivot"));

            creation_size = size;
            shapeGO = create_pivot_shape();
            AppendNewGO(shapeGO, pivotGO, false);

            pivotGO.AddChild(shapeGO);

            if (frameMaterial != null) {
                this.frameMaterial = frameMaterial;

                fMaterial mat = MaterialUtil.ToMaterialf(frameMaterial);
                frameGO = UnityUtil.CreateMeshGO("pivotFrame", "icon_meshes/axis_frame", size,
                    UnityUtil.MeshAlignOption.NoAlignment, mat, false);
                MaterialUtil.SetIgnoreMaterialChanges(frameGO);
                MaterialUtil.DisableShadows(frameGO);
                AppendNewGO(frameGO, pivotGO, false);
            }

            if (nShapeLayer >= 0)
                shapeGO.SetLayer(nShapeLayer);

            increment_timestamp();
            return this;
        }


        protected virtual fGameObject create_pivot_shape()
        {
            fGameObject go = AppendUnityPrimitiveGO("pivotMesh", UnityEngine.PrimitiveType.Sphere, CurrentMaterial, null, true);
            go.SetLocalScale(Size * Vector3f.One);
            return go;
        }


        protected virtual fGameObject PivotShapeGO {
            get { return shapeGO; }
        }

        public DMesh3 GetPivotShapeMesh() {
            return UnityUtil.UnityMeshToDMesh(shapeGO.GetSharedMesh(), false);
        }

        override public void DisableShadows() {
            MaterialUtil.DisableShadows(shapeGO, true, true);
            if ( frameGO != null )
                MaterialUtil.DisableShadows(frameGO, true, true);
        }




        //
        // SceneObject impl
        //

        override public fGameObject RootGameObject
        {
            get { return pivotGO; }
        }

        override public string Name
        {
            get { return pivotGO.GetName(); }
            set { pivotGO.SetName(value); }
        }

        override public SOType Type { get { return SOTypes.Pivot; } }

        public override bool IsSurface {
            get { return false; }
        }

        public override bool SupportsScaling {
            get { return false; }
        }

        public override void PreRender()
        {
            float fixed_scale = size / creation_size;

            float fScaling = 1.0f;
            if (MaintainConsistentViewSize) {
                fScaling = VRUtil.GetVRRadiusForVisualAngle(
                    pivotGO.GetPosition(),
                    parentScene.ActiveCamera.GetPosition(),
                    SceneGraphConfig.DefaultPivotVisualDegrees);
                fScaling /= parentScene.GetSceneScale();
                fScaling *= fixed_scale;
            } else {
                fScaling = fixed_scale;
            }
            pivotGO.SetLocalScale(new Vector3f(fScaling, fScaling, fScaling));
        }


        override public SceneObject Duplicate()
        {
            PivotSO copy = new PivotSO();
            copy.parentScene = this.parentScene;
            copy.Create(this.GetAssignedSOMaterial(), this.frameMaterial, shapeGO.GetLayer());
            copy.SetLocalFrame(
                this.GetLocalFrame(CoordSpace.ObjectCoords), CoordSpace.ObjectCoords);
            copy.MaintainConsistentViewSize = this.MaintainConsistentViewSize;
            return copy;
        }




        // SpatialQueryableSO impl

        public virtual bool SupportsNearestQuery { get { return true; } }
        public virtual bool FindNearest(Vector3d point, double maxDist, out SORayHit nearest, CoordSpace eInCoords)
        {
            nearest = null;

            Frame3f f = this.GetLocalFrame(eInCoords);
            double dist = (f.Origin - point).Length;
            if (dist > maxDist)
                return false;

            nearest = new SORayHit();
            nearest.fHitDist = (float)dist;
            nearest.hitPos = f.Origin;
            nearest.hitNormal = Vector3f.Zero;
            nearest.hitGO = RootGameObject;
            nearest.hitSO = this;
            return true;
        }



    }

}
