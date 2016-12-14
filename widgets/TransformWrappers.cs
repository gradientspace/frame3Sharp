using System;
using System.Collections.Generic;
using UnityEngine;
using g3;

namespace f3
{

    public interface ITransformWrapper : ITransformable
    {
        void BeginTransformation();
        void DoneTransformation();
        TransformableSceneObject Target { get; }
    }

    public abstract class BaseTransformWrapper : ITransformWrapper
    {
        abstract public bool SupportsScaling { get; }
        abstract public Frame3f GetLocalFrame(CoordSpace eSpace);
        abstract public Vector3 GetLocalScale();
        abstract public void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace);
        abstract public void SetLocalScale(Vector3 scale);

        protected TransformableSceneObject target;
        virtual public TransformableSceneObject Target { get { return target; } }


        TransformGizmoChange curChange;

        virtual public void BeginTransformation()
        {
            curChange = new TransformGizmoChange();
            curChange.parentSO = new WeakReference(Target);
            curChange.parentBefore = GetLocalFrame(CoordSpace.SceneCoords);
            curChange.parentScaleBefore = GetLocalScale();

            if (target.IsTemporary) {
                curChange.childSOs = new List<TransformableSceneObject>();
                SceneUtil.FindAllPersistentTransformableChildren(target, curChange.childSOs);
                curChange.before = new List<Frame3f>();
                curChange.scaleBefore = new List<Vector3>();
                foreach (TransformableSceneObject so in curChange.childSOs) {
                    curChange.before.Add(so.GetLocalFrame(CoordSpace.SceneCoords));
                    curChange.scaleBefore.Add(UnityUtil.GetFreeLocalScale(so.RootGameObject));
                }
            }
        }

        virtual public void DoneTransformation()
        {
            curChange.parentAfter = GetLocalFrame(CoordSpace.SceneCoords);
            curChange.parentScaleAfter = GetLocalScale();
            if (target.IsTemporary) {
                curChange.after = new List<Frame3f>();
                curChange.scaleAfter = new List<Vector3>();
                foreach (TransformableSceneObject so in curChange.childSOs) {
                    curChange.after.Add(so.GetLocalFrame(CoordSpace.SceneCoords));
                    curChange.scaleAfter.Add(UnityUtil.GetFreeLocalScale(so.RootGameObject));
                }
            }
            target.GetScene().History.PushChange(curChange, true);
            curChange = null;
            target.GetScene().History.PushInteractionCheckpoint();
        }

    }

    public class PassThroughWrapper : BaseTransformWrapper
    {
        public PassThroughWrapper(TransformableSceneObject target)
        {
            this.target = target;
        }
        override public void BeginTransformation()
        {
            base.BeginTransformation();
        }
        override public void DoneTransformation()
        {
            base.DoneTransformation();
        }
        override public Frame3f GetLocalFrame(CoordSpace eSpace)
        {
            return target.GetLocalFrame(eSpace);
        }
        override public void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace)
        {
            target.SetLocalFrame(newFrame, eSpace);
        }

        override public bool SupportsScaling
        {
            get { return target.SupportsScaling; }
        }
        override public Vector3 GetLocalScale()
        {
            return target.GetLocalScale();
        }
        override public void SetLocalScale(Vector3 scale)
        {
            target.SetLocalScale(scale);
        }
    }



    // This wrapper provides a frame aligned with the scene axes, and transforms translations/rotations
    // to the objects local frame. However it is a bit tricky to get the behavior right...currently
    // we are applying rotations to the scene frame within a Begin/DoneTransformation pair, and then
    // snapping back to world-aligned axes on DoneTransformation()
    public class SceneFrameWrapper : BaseTransformWrapper
    {
        FScene parentScene;

        public SceneFrameWrapper(FScene scene, TransformableSceneObject target)
        {
            this.parentScene = scene;
            this.target = target;
        }

        // [RMS] one ugly bit here is that we are calling BeginCapture before BeginTransformation,
        //   which may call GetLocalFrame before BeginTransformation has a change to initialize,
        //   so we have to initialize curRotation explicitly. 

        Frame3f objectFrame = Frame3f.Identity;
        Quaternionf curRotation = Quaternionf.Identity;

        override public void BeginTransformation()
        {
            base.BeginTransformation();
            objectFrame = target.GetLocalFrame(CoordSpace.ObjectCoords);
            curRotation = Quaternionf.Identity;
        }
        override public void DoneTransformation()
        {
            base.DoneTransformation();
            curRotation = Quaternionf.Identity;
        }

        override public Frame3f GetLocalFrame(CoordSpace eSpace)
        {
            Frame3f targetFrame = target.GetLocalFrame(eSpace);
            if (eSpace == CoordSpace.WorldCoords) {
                return new Frame3f(targetFrame.Origin, parentScene.RootGameObject.transform.rotation);
            } else if (eSpace == CoordSpace.SceneCoords) {
                return new Frame3f(targetFrame.Origin, parentScene.RootGameObject.transform.localRotation);
            } else {
                return new Frame3f(targetFrame.Origin, curRotation * Quaternionf.Identity);
            }
        }

        override public void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace)
        {
            Debug.Assert(eSpace == CoordSpace.ObjectCoords);

            Frame3f updateFrame = objectFrame;
            curRotation = newFrame.Rotation;
            updateFrame.Rotation = curRotation * objectFrame.Rotation;
            updateFrame.Origin = newFrame.Origin;
            target.SetLocalFrame(updateFrame, eSpace);
        }

        override public bool SupportsScaling
        {
            get { return target.SupportsScaling; }
        }
        override public Vector3 GetLocalScale()
        {
            return target.GetLocalScale();
        }
        override public void SetLocalScale(Vector3 scale)
        {
            target.SetLocalScale(scale);
        }

    }
}
