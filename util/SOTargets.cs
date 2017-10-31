using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class SOWorldIntersectionTarget : IIntersectionTarget
    {
        public SceneObject Target;

        public SOWorldIntersectionTarget(SceneObject target)
        {
            Target = target;
        }

        public virtual bool HasNormal { get { return true; } }

        public virtual bool RayIntersect(Ray3d ray, out Vector3d vHit, out Vector3d vHitNormal)
        {
            vHit = vHitNormal = Vector3d.Zero; 
            SORayHit hit;
            if ( Target.FindRayIntersection((Ray3f)ray, out hit) ) {
                vHit = hit.hitPos;
                vHitNormal = hit.hitNormal;
                return true;
            }
            return false;
        }
    }




    public class SOProjectionTarget : IProjectionTarget
    {
        public SpatialQueryableSO Target;
        public CoordSpace eInCoords = CoordSpace.WorldCoords;  // coord space of query and result point

        public float Offset = 0.0f;

        public SOProjectionTarget() { }
        public SOProjectionTarget(SpatialQueryableSO so, CoordSpace eCoordSpace = CoordSpace.WorldCoords)
        {
            Target = so;
            eInCoords = eCoordSpace;
        }

        public Vector3d Project(Vector3d vPoint, int identifier = -1)
        {
            SORayHit nearest;
            Target.FindNearest(vPoint, double.MaxValue, out nearest, eInCoords);
            return nearest.hitPos + Offset * nearest.hitNormal;
        }
    }


}
