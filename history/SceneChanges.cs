using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public class DeleteSOChange : BaseChangeOp
    {
        public FScene scene;
        public SceneObject so;

        public override string Identifier() { return "DeleteSOChange"; }

        public override OpStatus Apply() {
            // false here means we keep this GO around
            scene.RemoveSceneObject(so, false);
            return OpStatus.Success;        
        }

        public override OpStatus Revert() {
            scene.RestoreDeletedSceneObject(so);
            return OpStatus.Success;
        }

        public override OpStatus Cull() {
            scene.CullDeletedSceneObject(so);
            return OpStatus.Success;
        }
    }



    //
    // [TODO] in the case where we might replay Apply() after object
    //   has been deleted, the bKeepWorldPosition will not work!
    //
    public class AddSOChange : BaseChangeOp
    {
        public FScene scene;
        public SceneObject so;
        public bool bKeepWorldPosition = false;

        public override string Identifier() { return "AddSOChange"; }

        public override OpStatus Apply() {
            if (scene.HasDeletedSceneObject(so))
                scene.RestoreDeletedSceneObject(so);
            else
                scene.AddSceneObject(so, bKeepWorldPosition);
            return OpStatus.Success;
        }

        public override OpStatus Revert() {
            scene.RemoveSceneObject(so, false);
            return OpStatus.Success;
        }

        public override OpStatus Cull() {
            if ( scene.HasDeletedSceneObject(so) )
                scene.CullDeletedSceneObject(so);
            return OpStatus.Success;
        }
    }





    public class PrimitiveSOParamChange<T> : BaseChangeOp
    {
        public PrimitiveSO so;
        public string paramName;
        public T before, after;

        public override string Identifier() { return "PrimitiveSOParamChange"; }

        public override OpStatus Apply() {
            so.Parameters.SetValue(paramName, after);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            so.Parameters.SetValue(paramName, before);
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
            return OpStatus.Success;
        }
    }





    public class TransformSOChange : BaseChangeOp
    {
        public TransformableSO so;
        public Frame3f before, after;

        public override string Identifier() { return "TransformSOChange"; }

        public override OpStatus Apply() {
            so.SetLocalFrame(after, CoordSpace.SceneCoords);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            so.SetLocalFrame(before, CoordSpace.SceneCoords);
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
            return OpStatus.Success;
        }
    }





    public class TransformGizmoChange : BaseChangeOp
    {
        public WeakReference parentSO;
        public Frame3f parentBefore, parentAfter;
        public Vector3f parentScaleBefore, parentScaleAfter;

        public List<TransformableSO> childSOs;
        public List<Frame3f> before, after;
        public List<Vector3f> scaleBefore, scaleAfter;

        public override string Identifier() { return "TransformSOChange"; }

        public override OpStatus Apply()
        {
            // [RMS] parentSO may not be GC'd immediately, but GO be null as
            //   soon as Scene.RemoveSeceneObject() is called
            if ( parentSO.IsAlive && (parentSO.Target as SceneObject).RootGameObject != null) {
                TransformableSO tso = (parentSO.Target as TransformableSO);
                tso.SetLocalFrame(parentAfter, CoordSpace.SceneCoords);
                if (tso.SupportsScaling)
                    tso.SetLocalScale(parentScaleAfter);
            } else {
                for (int i = 0; i < childSOs.Count; ++i) {
                    childSOs[i].SetLocalFrame(after[i], CoordSpace.SceneCoords);
                    if (childSOs[i].SupportsScaling)
                        childSOs[i].SetLocalScale(scaleAfter[i]);
                }
            }
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            // [RMS] parentSO may not be GC'd immediately, but GO be null as
            //   soon as Scene.RemoveSeceneObject() is called
            if (parentSO.IsAlive && (parentSO.Target as SceneObject).RootGameObject != null  ) {
                TransformableSO tso = (parentSO.Target as TransformableSO);
                tso.SetLocalFrame(parentBefore, CoordSpace.SceneCoords);
                if (tso.SupportsScaling)
                    tso.SetLocalScale(parentScaleBefore);
            } else {
                for (int i = 0; i < childSOs.Count; ++i) {
                    childSOs[i].SetLocalFrame(before[i], CoordSpace.SceneCoords);
                    if (childSOs[i].SupportsScaling)
                        childSOs[i].SetLocalScale(scaleBefore[i]);
                }

            }
            return OpStatus.Success;
        }
        public override OpStatus Cull()
        {
            return OpStatus.Success;
        }
    }





    public class SOMaterialChange : BaseChangeOp
    {
        public SceneObject so;
        public SOMaterial before, after;

        public override string Identifier() { return "SOMaterialChange"; }

        public override OpStatus Apply()
        {
            so.AssignSOMaterial(after);
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            so.AssignSOMaterial(before);
            return OpStatus.Success;
        }
        public override OpStatus Cull()
        {
            return OpStatus.Success;
        }
    }

}
