using System;
using System.Collections.Generic;
using System.Linq;
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
        public SceneObject so;
        public Frame3f before, after;
        public CoordSpace space;

        public override string Identifier() { return "TransformSOChange"; }

        public TransformSOChange() {
            space = CoordSpace.SceneCoords;
        }
        public TransformSOChange(SceneObject target, Frame3f before, Frame3f after, CoordSpace coords) {
            this.so = target;
            this.before = before;
            this.after = after;
            this.space = coords;
        }
        public TransformSOChange(SceneObject target, Frame3f newFrame, CoordSpace coords)
        {
            this.so = target;
            this.before = target.GetLocalFrame(coords);
            this.after = newFrame;
            this.space = coords;
        }

        public override OpStatus Apply() {
            so.SetLocalFrame(after, space);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            so.SetLocalFrame(before, space);
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

        public List<SceneObject> childSOs;
        public List<Frame3f> before, after;
        public List<Vector3f> scaleBefore, scaleAfter;

        public override string Identifier() { return "TransformSOChange"; }

        public override OpStatus Apply()
        {
            // [RMS] parentSO may not be GC'd immediately, but GO may be destroyed
            //   soon as Scene.RemoveSceneObject() is called
            SceneObject useParentSO = null;
            if (parentSO.IsAlive) {
                useParentSO = (parentSO.Target as SceneObject);
                if (useParentSO == null || useParentSO.RootGameObject == null || useParentSO.RootGameObject.IsDestroyed == true)
                    useParentSO = null;
            }

            if (useParentSO != null ) {
                useParentSO.SetLocalFrame(parentAfter, CoordSpace.SceneCoords);
                if (useParentSO.SupportsScaling)
                    useParentSO.SetLocalScale(parentScaleAfter);
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
            // [RMS] parentSO may not be GC'd immediately, but GO may be destroyed
            //   soon as Scene.RemoveSceneObject() is called
            SceneObject useParentSO = null;
            if (parentSO.IsAlive) {
                useParentSO = (parentSO.Target as SceneObject);
                if (useParentSO == null || useParentSO.RootGameObject == null || useParentSO.RootGameObject.IsDestroyed == true)
                    useParentSO = null;
            }

            if (useParentSO != null) {
                useParentSO.SetLocalFrame(parentBefore, CoordSpace.SceneCoords);
                if (useParentSO.SupportsScaling)
                    useParentSO.SetLocalScale(parentScaleBefore);
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






    public class CreateGroupChange : BaseChangeOp
    {
        public FScene Scene;
        public List<SceneObject> Objects;

        // [RMS] ugh this is terrible....but how else do we hold refs to SOs??
        //  (Could we store UUID?)
        GroupSO created_group;

        public override string Identifier() { return "CreateGroupChange"; }

        public override OpStatus Apply() {
            if (created_group == null) {
                created_group = new GroupSO();
                created_group.Create();
                Scene.AddSceneObject(created_group, false);
            } else {
                Scene.RestoreDeletedSceneObject(created_group);
            }
            created_group.AddChildren(Objects);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            created_group.RemoveAllChildren();
            Scene.RemoveSceneObject(created_group, false);
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
            Scene.CullDeletedSceneObject(created_group);
            return OpStatus.Success;
        }
    }




    public class AddToGroupChange : BaseChangeOp
    {
        public FScene Scene;
        public GroupSO Group;
        public List<SceneObject> Objects;

        public override string Identifier() { return "AddToGroupChange"; }

        public AddToGroupChange(FScene scene, GroupSO group, SceneObject so)
        {
            this.Scene = scene; this.Group = group; this.Objects = new List<SceneObject>() { so };
        }

        public override OpStatus Apply() {
            Group.AddChildren(Objects, true);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            foreach (var so in Objects)
                Group.RemoveChild(so, true);
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
            return OpStatus.Success;
        }
    }






    public class UnGroupChange : BaseChangeOp
    {
        public FScene Scene;
        public GroupSO Group;

        List<SceneObject> Objects;

        public override string Identifier() { return "UnGroupChange"; }

        public override OpStatus Apply() {
            Objects = new List<SceneObject>(Group.GetChildren());
            Group.RemoveAllChildren();
            Scene.RemoveSceneObject(Group, false);
            return OpStatus.Success;
        }
        public override OpStatus Revert() {
            Scene.RestoreDeletedSceneObject(Group);
            Group.AddChildren(Objects);
            return OpStatus.Success;
        }
        public override OpStatus Cull() {
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
