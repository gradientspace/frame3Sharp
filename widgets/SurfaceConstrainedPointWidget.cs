using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{
    //
    // 
    // 
    public class SurfaceConstrainedPointWidget : Standard3DWidget
    {
        //ITransformGizmo parent;
        FScene Scene;

        public Frame3f SourceFrameL;             // frame of widget in local coords of gizmo

        // you should set these on/around BeginCapture
        public SceneObject SourceSO;
        public List<SceneObject> ConstraintSurfaces;

        public SceneObject CurrentConstraintSO;

        public SurfaceConstrainedPointWidget(ITransformGizmo parent, FScene scene)
        {
            //this.parent = parent;
            this.Scene = scene;
        }

        public override bool BeginCapture(ITransformable target, Ray3f worldRay, UIRayHit hit)
        {
            return true;
        }


        public override bool UpdateCapture(ITransformable target, Ray3f worldRay)
        {
            Func<SceneObject, bool> FilterF = (so) => {
                return this.ConstraintSurfaces.Contains(so);
            };

            SORayHit hit;
            if ( Scene.FindSORayIntersection(worldRay, out hit, FilterF) ) {
                CurrentConstraintSO = hit.hitSO;
                Frame3f f = SourceSO.GetLocalFrame(CoordSpace.WorldCoords);
                f.Origin = hit.hitPos;
                f.AlignAxis(2, hit.hitNormal);
                SourceSO.SetLocalFrame(f, CoordSpace.WorldCoords);

            }

            return true;
        }

        public override bool EndCapture(ITransformable target)
        {
            return true;
        }

        public override void Disconnect()
        {
            RootGameObject.Destroy();
        }
    }
}

