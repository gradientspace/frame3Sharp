using System;
using System.Collections.Generic;
using g3;
using f3;

namespace f3
{
    /// <summary>
    /// completely replaces internal mesh
    /// </summary>
    public class ReplaceEntireMeshChange : BaseChangeOp
    {
        public override string Identifier() { return "ReplaceEntireMeshChange"; }

        public DMeshSO Target;
        public DMesh3 Before;    // [TODO] replace w/ packed variant that uses less memory (can we? what about edge IDs? maybe just add 'compress' function?)
        public DMesh3 After;

        public ReplaceEntireMeshChange(DMeshSO target, DMesh3 before, DMesh3 after)
        {
            Target = target;
            Before = before;
            After = after;
        }

        public override OpStatus Apply()
        {
            Target.ReplaceMesh(After, false);
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            Target.ReplaceMesh(Before, false);
            return OpStatus.Success;
        }

        public override OpStatus Cull()
        {
            Target = null;
            Before = After = null;
            return OpStatus.Success;
        }
    }




    /// <summary>
    /// Undoable call to DMeshSO.RepositionPivot
    /// </summary>
    public class RepositionPivotChangeOp : BaseChangeOp
    {
        public override string Identifier() { return "RepositionPivotChange"; }

        public DMeshSO Target;

        Frame3f initialFrame;
        Frame3f toFrame;

        public RepositionPivotChangeOp(Frame3f toPivot, DMeshSO target)
        {
            Target = target;
            toFrame = toPivot;
            initialFrame = Target.GetLocalFrame(CoordSpace.ObjectCoords);
        }

        public override OpStatus Apply()
        {
            Target.RepositionPivot(toFrame);
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            Target.RepositionPivot(initialFrame);
            return OpStatus.Success;
        }

        public override OpStatus Cull()
        {
            Target = null;
            return OpStatus.Success;
        }
    }
}
