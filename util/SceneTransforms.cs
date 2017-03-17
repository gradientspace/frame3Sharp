using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public static class SceneTransforms
    {

        /// <summary>
        /// Assuming pointIn is in space eFrom of fromSO, transform to eTo
        /// </summary>
        public static Vector3f TransformTo(Vector3f pointIn, TransformableSO fromSO, CoordSpace eFrom, CoordSpace eTo)
        {
            // this is not the most efficient but we can optimize later!
            Frame3f tmp = new Frame3f(pointIn);
            return TransformTo(tmp, fromSO, eFrom, eTo).Origin;
        }


        /// <summary>
        /// Assuming frameIn is in space eFrom of fromSO, transform to eTo
        /// </summary>
        public static Frame3f TransformTo(Frame3f frameIn, TransformableSO fromSO, CoordSpace eFrom, CoordSpace eTo)
        {
            if (eFrom == eTo)
                return frameIn;

            FScene scene = fromSO.GetScene();

            Frame3f sceneF = frameIn;
            if (eFrom == CoordSpace.ObjectCoords) {
                // if we are in Object coords, we are either going up to Scene or World
                sceneF = ObjectToScene(fromSO, frameIn);

            } else if (eFrom == CoordSpace.WorldCoords) {
                // if we are in World coords, we are going down to Scene or Object
                sceneF = scene.ToSceneFrame(frameIn);

            } // (otherwise frameIn is in Scene coords)

            // going World->Scene or Object->Scene
            if (eTo == CoordSpace.SceneCoords)
                return sceneF;

            // going Scene->World or Object->World
            if (eTo == CoordSpace.WorldCoords)
                return scene.ToWorldFrame(sceneF);

            // only thing left is going from Scene to Object
            return SceneToObject(fromSO, sceneF);
        }



        /// <summary>
        /// Apply the local rotate/translate/scale at transform to frameIn
        /// </summary>
        public static Frame3f ApplyTransform(ITransformed transform, Frame3f frameIn )
        {
            Frame3f result = frameIn.Scaled(transform.GetLocalScale());
            return transform.GetLocalFrame(CoordSpace.ObjectCoords).FromFrame(result);
        }


        /// <summary>
        /// Apply the inverse local rotate/translate/scale at transform to frameIn
        /// </summary>
        public static Frame3f ApplyInverseTransform(ITransformed transform, Frame3f frameIn )
        {
            Frame3f result = transform.GetLocalFrame(CoordSpace.ObjectCoords).ToFrame(frameIn);
            result.Scale(1.0f / transform.GetLocalScale());
            return result;
        }


        /// <summary>
        /// Input sceneF is a frame in Scene, apply all intermediate inverse 
        /// transforms to get it into local frame of a SO
        /// </summary>
        public static Frame3f SceneToObject(TransformableSO so, Frame3f sceneF)
        {
            SOParent parent = so.Parent;
            if (parent is FScene)
                return ApplyInverseTransform(so, sceneF);
            // this will recursively apply all the inverse parent transforms from scene on down
            return ApplyInverseTransform(so, SceneToObject(parent as TransformableSO, sceneF));
        }

        /// <summary>
        /// input objectF is in Object (local) coords of so, apply all intermediate 
        /// transforms to get it to Scene coords
        /// </summary>
        public static Frame3f ObjectToScene(TransformableSO so, Frame3f objectF)
        {
            Frame3f sceneF = objectF;
            TransformableSO curSO = so;
            while (curSO != null) {
                Frame3f curF = curSO.GetLocalFrame(CoordSpace.ObjectCoords);
                Vector3f scale = curSO.GetLocalScale();
                sceneF.Scale(scale);
                sceneF = curF.FromFrame(sceneF);
                SOParent parent = curSO.Parent;
                if (parent is FScene)
                    return sceneF;
                curSO = (parent as TransformableSO);
            }
            if (curSO == null)
                DebugUtil.Error("SceneTransforms.TransformTo: found null parent SO!");
            return sceneF;
        }

    }
}
