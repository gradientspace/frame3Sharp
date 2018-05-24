using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public static class SceneTransforms
    {

        //
        // WARNING!! These functions do not support non-uniform scaling. 
        //
        //
        public static bool IsUniformScale(Vector3f s)
        {
            return MathUtil.EpsilonEqual(s[0], s[1], 0.0001f) && MathUtil.EpsilonEqual(s[1], s[2], 0.0001f);
        }




        /// <summary>
        /// Assuming pointIn is in space eFrom of fromSO, transform to eTo
        /// </summary>
        public static Vector3f TransformTo(Vector3f pointIn, SceneObject fromSO, CoordSpace eFrom, CoordSpace eTo)
        {
            // this is not the most efficient but we can optimize later!
            Frame3f f = new Frame3f(pointIn);
            Frame3f fTo = TransformTo(f, fromSO, eFrom, eTo);
            return fTo.Origin;
        }


        /// <summary>
        /// Assuming frameIn is in space eFrom of fromSO, transform to eTo
        /// </summary>
        public static Frame3f TransformTo(Frame3f frameIn, SceneObject fromSO, CoordSpace eFrom, CoordSpace eTo)
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
        /// Assuming dimensionIn is in space eFrom of fromSO, transform to eTo
        /// </summary>
        public static float TransformTo(float dimensionIn, SceneObject fromSO, CoordSpace eFrom, CoordSpace eTo)
        {
            if (eFrom == eTo)
                return dimensionIn;

            FScene scene = fromSO.GetScene();

            float sceneDim = dimensionIn;
            if (eFrom == CoordSpace.ObjectCoords) {
                // if we are in Object coords, we are either going up to Scene or World
                sceneDim = ObjectToScene(fromSO, dimensionIn);

            } else if (eFrom == CoordSpace.WorldCoords) {
                // if we are in World coords, we are going down to Scene or Object
                sceneDim = scene.ToSceneDimension(dimensionIn);

            } // (otherwise frameIn is in Scene coords)

            // going World->Scene or Object->Scene
            if (eTo == CoordSpace.SceneCoords)
                return sceneDim;

            // going Scene->World or Object->World
            if (eTo == CoordSpace.WorldCoords)
                return scene.ToWorldDimension(sceneDim);

            // only thing left is going from Scene to Object
            return SceneToObject(fromSO, sceneDim);
        }





        /// <summary>
        /// transform frame from Object coords of fromSO into Object coords of toSO
        /// </summary>
        public static Frame3f TransformTo(Frame3f frameIn, SceneObject fromSO, SceneObject toSO)
        {
            Frame3f frameS = TransformTo(frameIn, fromSO, CoordSpace.ObjectCoords, CoordSpace.SceneCoords);
            return TransformTo(frameS, toSO, CoordSpace.SceneCoords, CoordSpace.ObjectCoords);
        }

        /// <summary>
        /// transform point from Object coords of fromSO into Object coords of toSO
        /// </summary>
        public static Vector3f TransformTo(Vector3f ptIn, SceneObject fromSO, SceneObject toSO)
        {
            Frame3f frameS = TransformTo(new Frame3f(ptIn), fromSO, CoordSpace.ObjectCoords, CoordSpace.SceneCoords);
            return TransformTo(frameS, toSO, CoordSpace.SceneCoords, CoordSpace.ObjectCoords).Origin;
        }

        /// <summary>
        /// transform point from Object coords of fromSO into Object coords of toSO
        /// </summary>
        public static Vector3d TransformTo(Vector3d ptIn, SceneObject fromSO, SceneObject toSO)
        {
            Frame3f frameS = TransformTo(new Frame3f((Vector3f)ptIn), fromSO, CoordSpace.ObjectCoords, CoordSpace.SceneCoords);
            return TransformTo(frameS, toSO, CoordSpace.SceneCoords, CoordSpace.ObjectCoords).Origin;
        }



        /// <summary>
        /// Apply the local rotate/translate/scale at transform to frameIn
        /// </summary>
        public static Frame3f ApplyTransform(ITransformed transform, Frame3f frameIn )
        {
            Frame3f result = frameIn.Scaled(transform.GetLocalScale());
            return transform.GetLocalFrame(CoordSpace.ObjectCoords).FromFrame(ref result);
        }


        /// <summary>
        /// Apply the inverse local rotate/translate/scale at transform to frameIn
        /// </summary>
        public static Frame3f ApplyInverseTransform(ITransformed transform, Frame3f frameIn )
        {
            Frame3f result = transform.GetLocalFrame(CoordSpace.ObjectCoords).ToFrame(ref frameIn);
            Vector3f scale = transform.GetLocalScale();
            Util.gDevAssert(IsUniformScale(scale));
            result.Scale(1.0f / scale);
            return result;
        }



        /// <summary>
        /// Cache the transform sequence from scene coordinates down to SO-local coordinates
        /// </summary>
        public static TransformSequence SceneToObjectXForm(SceneObject so)
        {
            // [TODO] could be more efficient?
            return ObjectToSceneXForm(so).MakeInverse();
        }



        /// <summary>
        /// construct cache of object-to-object xform
        /// </summary>
        public static TransformSequence ObjectToObjectXForm(SceneObject fromSO, SceneObject toSO)
        {
            TransformSequence fromToScene = ObjectToSceneXForm(fromSO);
            TransformSequence sceneToTarget = SceneToObjectXForm(toSO);
            fromToScene.Append(sceneToTarget);
            return fromToScene;
        }



        /// <summary>
        /// Input sceneF is a frame in Scene, apply all intermediate inverse 
        /// transforms to get it into local frame of a SO
        /// </summary>
        public static Frame3f SceneToObject(SceneObject so, Frame3f sceneF)
        {
            SOParent parent = so.Parent;
            if (parent is FScene)
                return ApplyInverseTransform(so, sceneF);
            // this will recursively apply all the inverse parent transforms from scene on down
            return ApplyInverseTransform(so, SceneToObject(parent as SceneObject, sceneF));
        }


        /// <summary>
        /// Input ray is a frame in Scene, apply all intermediate inverse 
        /// transforms to get it into local frame of a SO
        /// </summary>
        public static Ray3f SceneToObject(SceneObject so, Ray3f ray)
        {
            Frame3f f = new Frame3f(ray.Origin, ray.Direction);
            Frame3f fO = SceneToObject(so, f);
            return new Ray3f(fO.Origin, fO.Z);
        }


        /// <summary>
        /// input box is in Scene, apply all intermediate inverse
        /// transforms to get it to local frame of SO
        /// </summary>
        public static Box3f SceneToObject(SceneObject so, Box3f box)
        {
            Frame3f f = new Frame3f(box.Center, box.AxisX, box.AxisY, box.AxisZ);
            Frame3f fL = SceneToObject(so, f);

            // [TODO] make more efficient...
            Vector3f dv = box.Extent.x*box.AxisX + box.Extent.y*box.AxisY + box.Extent.z*box.AxisZ;
            Frame3f fCornerS = new Frame3f(box.Center + dv);
            Frame3f fCornerL = SceneToObject(so, fCornerS);
            Vector3f dvL = fCornerL.Origin - fL.Origin;
            Vector3f scales = new Vector3f(
                dvL.Dot(fCornerL.X) / box.Extent.x,
                dvL.Dot(fCornerL.Y) / box.Extent.y,
                dvL.Dot(fCornerL.Z) / box.Extent.z);
            
            return new Box3f(fL.Origin, fL.X, fL.Y, fL.Z, scales * box.Extent);
        }


        /// <summary>
        /// Input sceneF is a point in Scene, apply all intermediate inverse 
        /// transforms to get it into local point of a SO
        /// </summary>
        public static Vector3f SceneToObjectP(SceneObject so, Vector3f scenePt)
        {
            Frame3f f = new Frame3f(scenePt);
            Frame3f fO = SceneToObject(so, f);
            return fO.Origin;
        }
        public static Vector3d SceneToObjectP(SceneObject so, Vector3d scenePt)
        {
            return (Vector3d)SceneToObjectP(so, (Vector3f)scenePt);
        }

        /// <summary>
        /// Input sceneN is a normal vector in Scene, apply all intermediate inverse 
        /// transforms to get it into local point of a SO. **NO SCALING**
        /// </summary>
        public static Vector3f SceneToObjectN(SceneObject so, Vector3f sceneN)
        {
            Frame3f f = new Frame3f(Vector3f.Zero, sceneN);
            Frame3f fO = SceneToObject(so, f);
            return fO.Z;
        }

        /// <summary>
        /// input dimension is in scene coords, (recursively) apply all 
        /// intermediate inverse-scales to get it into local coords of SO.
        /// </summary>
        public static float SceneToObject(SceneObject so, float sceneDim)
        {
            return inverse_scale_recursive(so, sceneDim);
        }
        static float inverse_scale_recursive(SceneObject so, float dim)
        {
            Vector3f scale = so.GetLocalScale();
            Util.gDevAssert(IsUniformScale(scale));
            float avgscale = ((scale.x + scale.y + scale.z) / 3.0f);   // yikes!
            if (so.Parent is FScene)
                return dim / avgscale;
            else
                return inverse_scale_recursive(so.Parent as SceneObject, dim) / avgscale;
        }


        [System.Obsolete("Renamed to SceneToObjectP")]
        public static Vector3f SceneToObject(SceneObject so, Vector3f scenePt) { return SceneToObjectP(so, scenePt); }
        [System.Obsolete("Renamed to SceneToObjectP")]
        public static Vector3d SceneToObject(SceneObject so, Vector3d scenePt) { return SceneToObjectP(so, scenePt); }





        /// <summary>
        /// Cache the transform sequence from SO up to scene coordinates
        /// </summary>
        public static TransformSequence ObjectToSceneXForm(SceneObject so)
        {
            TransformSequence seq = new TransformSequence();
            SceneObject curSO = so;
            while (curSO != null) {
                Frame3f curF = curSO.GetLocalFrame(CoordSpace.ObjectCoords);
                Vector3f scale = curSO.GetLocalScale();
                seq.AppendScale(scale);
                seq.AppendFromFrame(curF);
                SOParent parent = curSO.Parent;
                if (parent is FScene)
                    break;
                curSO = (parent as SceneObject);
            }
            return seq;
        }



        /// <summary>
        /// input dimension is in Object (local) coords of so, apply all intermediate 
        /// transform scaling to get it to Scene coords
        /// </summary>
        public static float ObjectToScene(SceneObject so, float objectDim)
        {
            float sceneDim = objectDim;
            SceneObject curSO = so;
            while (curSO != null) {
                Vector3f scale = curSO.GetLocalScale();
                Util.gDevAssert(IsUniformScale(scale));
                sceneDim *= ( (scale.x+scale.y+scale.z) / 3.0f);   // yikes!
                SOParent parent = curSO.Parent;
                if (parent is FScene)
                    return sceneDim;
                curSO = (parent as SceneObject);
            }
            if (curSO == null)
                DebugUtil.Error("SceneTransforms.TransformTo: found null parent SO!");
            return sceneDim;
        }


        /// <summary>
        /// input objectF is in Object (local) coords of so, apply all intermediate 
        /// transforms to get it to Scene coords
        /// </summary>
        public static Frame3f ObjectToScene(SceneObject so, Frame3f objectF)
        {
            Frame3f sceneF = objectF;
            SceneObject curSO = so;
            while (curSO != null) {
                Frame3f curF = curSO.GetLocalFrame(CoordSpace.ObjectCoords);
                Vector3f scale = curSO.GetLocalScale();
                Util.gDevAssert(IsUniformScale(scale));
                sceneF.Scale(scale);
                sceneF = curF.FromFrame(ref sceneF);
                SOParent parent = curSO.Parent;
                if (parent is FScene)
                    return sceneF;
                curSO = (parent as SceneObject);
            }
            if (curSO == null)
                DebugUtil.Error("SceneTransforms.TransformTo: found null parent SO!");
            return sceneF;
        }

        /// <summary>
        /// input ray is in Object (local) coords of so, apply all intermediate 
        /// transforms to get it to Scene coords
        /// </summary>
        public static Ray3f ObjectToScene(SceneObject so, Ray3f ray)
        {
            Frame3f f = new Frame3f(ray.Origin, ray.Direction);
            Frame3f fS = ObjectToScene(so, f);
            return new Ray3f(fS.Origin, fS.Z);
        }


        /// <summary>
        /// input box is in Object (local) coords of so, apply all intermediate 
        /// transforms to get it to Scene coords
        /// </summary>
        public static Box3f ObjectToScene(SceneObject so, Box3f box)
        {
            Frame3f f = new Frame3f(box.Center, box.AxisX, box.AxisY, box.AxisZ);
            Frame3f fS = ObjectToScene(so, f);
            // [TODO] could maybe figure out nonuniform scaling by applying this to
            //   box-corner-pt vector instead, and taking dots before/after?
            float scale = ObjectToSceneV(so, Vector3f.OneNormalized).Length;
            return new Box3f(fS.Origin, fS.X, fS.Y, fS.Z, scale*box.Extent);
        }


        /// <summary>
        /// input objectF is in Object (local) coords of so, apply all intermediate 
        /// transforms to get it to Scene coords
        /// </summary>
        public static Vector3f ObjectToSceneP(SceneObject so, Vector3f objectPt)
        {
            Frame3f f = new Frame3f(objectPt);
            Frame3f fS = ObjectToScene(so, f);
            return fS.Origin;
        }
        public static Vector3d ObjectToSceneP(SceneObject so, Vector3d scenePt)
        {
            return (Vector3d)ObjectToSceneP(so, (Vector3f)scenePt);
        }


        /// <summary>
        /// Input objectN is a normal vector in local coords of SO, apply all intermediate inverse 
        /// transforms to get it into scene coords. **NO SCALING**
        /// </summary>
        public static Vector3f ObjectToSceneN(SceneObject so, Vector3f objectN)
        {
            Frame3f f = new Frame3f(Vector3f.Zero, objectN);
            Frame3f fO = ObjectToScene(so, f);
            return fO.Z;
        }


        /// <summary>
        /// Input objectV is a vector in local coords of SO, apply all intermediate inverse 
        /// transforms to get it into scene coords.
        /// </summary>
        public static Vector3f ObjectToSceneV(SceneObject so, Vector3f objectV)
        {
            Vector3f sceneV = objectV;
            SceneObject curSO = so;
            while (curSO != null) {
                Frame3f curF = curSO.GetLocalFrame(CoordSpace.ObjectCoords);
                Vector3f scale = curSO.GetLocalScale();
                sceneV *= scale;
                sceneV = curF.FromFrameV(ref sceneV);
                SOParent parent = curSO.Parent;
                if (parent is FScene)
                    return sceneV;
                curSO = (parent as SceneObject);
            }
            if (curSO == null)
                DebugUtil.Error("SceneTransforms.ObjectToSceneV: found null parent SO!");
            return sceneV;
        }


        [System.Obsolete("Renamed to SceneToObjectP")]
        public static Vector3f ObjectToScene(SceneObject so, Vector3f objectPt) { return ObjectToSceneP(so, objectPt); }
        [System.Obsolete("Renamed to SceneToObjectP")]
        public static Vector3d ObjectToScene(SceneObject so, Vector3d objectPt) { return ObjectToSceneP(so, objectPt); }





        /// <summary>
        /// convert input sceneF in Scene to World
        /// </summary>
        public static Frame3f SceneToWorld(FScene scene, Frame3f sceneF) {
            return scene.ToWorldFrame(sceneF);
        }
        public static Vector3f SceneToWorldP(FScene scene, Vector3f scenePt) {
            return scene.ToWorldP(scenePt);
        }
        public static Vector3d SceneToWorldP(FScene scene, Vector3d scenePt) {
            return scene.ToWorldP(scenePt);
        }
        public static Vector3f SceneToWorldN(FScene scene, Vector3f sceneN) {
            return scene.ToWorldN(sceneN);
        }
        public static Vector3d SceneToWorldN(FScene scene, Vector3d sceneN) {
            return scene.ToWorldN((Vector3f)sceneN);
        }

        [System.Obsolete("Renamed to SceneToWorldP")]
        public static Vector3f SceneToWorld(FScene scene, Vector3f scenePt) { return SceneToWorldP(scene, scenePt); }
        [System.Obsolete("Renamed to SceneToWorldP")]
        public static Vector3d SceneToWorld(FScene scene, Vector3d scenePt) { return SceneToWorldP(scene, scenePt); }


        /// <summary>
        /// convert input sceneF in World into Scene
        /// </summary>
        public static Frame3f WorldToScene(FScene scene, Frame3f sceneF) {
            return scene.ToSceneFrame(sceneF);
        }
        public static Vector3f WorldToSceneP(FScene scene, Vector3f scenePt) {
            return scene.ToSceneP(scenePt);
        }
        public static Vector3d WorldToSceneP(FScene scene, Vector3d scenePt) {
            return scene.ToSceneP(scenePt);
        }
        public static Vector3f WorldToSceneN(FScene scene, Vector3f sceneN) {
            return scene.ToSceneN(sceneN);
        }
        public static Vector3d WorldToSceneN(FScene scene, Vector3d sceneN) {
            return scene.ToSceneN((Vector3f)sceneN);
        }

        [System.Obsolete("Renamed to WorldToSceneP")]
        public static Vector3f WorldToScene(FScene scene, Vector3f scenePt) { return WorldToSceneP(scene, scenePt); }
        [System.Obsolete("Renamed to WorldToSceneP")]
        public static Vector3d WorldToScene(FScene scene, Vector3d scenePt) { return WorldToSceneP(scene, scenePt); }

    }
}
