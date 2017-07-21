using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public class LocalCurvePreview : CurvePreview
    {
        public TransformableSO Target;
        //int target_timestamp;

        struct LocalVertexRef
        {
            public Vector3d localPos;
        }
        List<LocalVertexRef> SurfacePoints;
        

        public LocalCurvePreview(TransformableSO targetSurf) : base()
        {
            Target = targetSurf;
            SurfacePoints = new List<LocalVertexRef>();
            //target_timestamp = Target.Timestamp;
        }


        public override void AppendVertex(Vector3d v)
        {
            base.AppendVertex(v);

            // map v to local coords
            LocalVertexRef r = new LocalVertexRef();
            r.localPos = SceneTransforms.SceneToObject(Target, v);
            SurfacePoints.Add(r);
            if (Curve.VertexCount != SurfacePoints.Count)
                throw new Exception("SurfaceCurvePreview: counts are out of sync!!");
        }


        protected override void update_vertices(FScene s)
        {
            // [RMS] this was commented out...doesn't work? something?
            //if (Target.Timestamp == target_timestamp)
            //    return;

            //target_timestamp = Target.Timestamp;

            for ( int i = 0; i < VertexCount; ++i ) {
                LocalVertexRef r = SurfacePoints[i];
                Vector3d vScene = SceneTransforms.ObjectToScene(Target, r.localPos);
                this[i] = vScene;
            }

        }



    }
}
