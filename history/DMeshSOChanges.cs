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
    /// Removes a set of triangles. You can initialize this in two ways, either
    /// by constructing your own remove change, or by calling ApplyInitialize(),
    /// which will compute the internal RemoveTrianglesMeshChange
    /// as it removes the triangles from the mesh. 
    /// </summary>
    public class RemoveTrianglesChange : BaseChangeOp
    {
        public override string Identifier() { return "RemoveTrianglesChange"; }

        public DMeshSO Target;
        public RemoveTrianglesMeshChange MeshChange;

        public RemoveTrianglesChange(DMeshSO target)
        {
            Target = target;
        }

        public RemoveTrianglesChange(DMeshSO target, RemoveTrianglesMeshChange change)
        {
            Target = target;
            MeshChange = change;
        }

        public void ApplyInitialize(IEnumerable<int> triangles)
        {
            if (MeshChange != null)
                throw new Exception("RemoveTrianglesChange.ApplyInitialize: change is already initialized!");
            MeshChange = new RemoveTrianglesMeshChange();
            Target.EditAndUpdateMesh(
                (mesh) => { MeshChange.InitializeFromApply(mesh, triangles); },
                GeometryEditTypes.ArbitraryEdit
            );
        }

        public override OpStatus Apply()
        {
            if (MeshChange == null)
                throw new Exception("RemoveTrianglesChange.Apply: Must call ApplyInitialize first!!");
            Target.EditAndUpdateMesh(
                (mesh) => { MeshChange.Apply(mesh); },
                GeometryEditTypes.ArbitraryEdit
            );
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            if (MeshChange == null)
                throw new Exception("RemoveTrianglesChange.Revert: Must call ApplyInitialize first!!");
            Target.EditAndUpdateMesh(
                (mesh) => { MeshChange.Revert(mesh); },
                GeometryEditTypes.ArbitraryEdit
            );
            return OpStatus.Success;
        }

        public override OpStatus Cull()
        {
            Target = null;
            MeshChange = null;
            return OpStatus.Success;
        }
    }






    /// <summary>
    /// Adds a set of triangles. 
    /// </summary>
    public class AddTrianglesChange : BaseChangeOp
    {
        public override string Identifier() { return "AddTrianglesChange"; }

        public DMeshSO Target;
        public AddTrianglesMeshChange MeshChange;

        public AddTrianglesChange(DMeshSO target, AddTrianglesMeshChange change)
        {
            Target = target;
            MeshChange = change;
        }

        public override OpStatus Apply()
        {
            Target.EditAndUpdateMesh(
                (mesh) => { MeshChange.Apply(mesh); },
                GeometryEditTypes.ArbitraryEdit
            );
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            Target.EditAndUpdateMesh(
                (mesh) => { MeshChange.Revert(mesh); },
                GeometryEditTypes.ArbitraryEdit
            );
            return OpStatus.Success;
        }

        public override OpStatus Cull()
        {
            Target = null;
            MeshChange = null;
            return OpStatus.Success;
        }
    }




    /// <summary>
    /// Replaces a set of triangles. 
    /// </summary>
    public class ReplaceTrianglesChange : BaseChangeOp
    {
        public override string Identifier() { return "ReplaceTrianglesChange"; }

        public DMeshSO Target;
        public RemoveTrianglesMeshChange RemoveChange;
        public AddTrianglesMeshChange AddChange;

        public ReplaceTrianglesChange(DMeshSO target, RemoveTrianglesMeshChange remove, AddTrianglesMeshChange add)
        {
            RemoveChange = remove;
            AddChange = add;
            Target = target;
        }

        public override OpStatus Apply()
        {
            Target.EditAndUpdateMesh(
                (mesh) => {
                    RemoveChange.Apply(mesh);
                    AddChange.Apply(mesh);
                },
                GeometryEditTypes.ArbitraryEdit
            );
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            Target.EditAndUpdateMesh(
                (mesh) => {
                    AddChange.Revert(mesh);
                    RemoveChange.Revert(mesh);
                },
                GeometryEditTypes.ArbitraryEdit
            );
            return OpStatus.Success;
        }

        public override OpStatus Cull()
        {
            Target = null;
            RemoveChange = null;
            AddChange = null;
            return OpStatus.Success;
        }
    }







    /// <summary>
    /// Wraps a mesh change that only modifies vertices (position, normal, color, uv).
    /// This is more much efficient in both memory and update cost than a ReplaceTrianglesChange.
    /// </summary>
    public class UpdateVerticesChange : BaseChangeOp
    {
        public override string Identifier() { return "UpdateVerticesChange"; }

        public DMeshSO Target;
        public ModifyVerticesMeshChange MeshChange;

        public UpdateVerticesChange(DMeshSO target, ModifyVerticesMeshChange change)
        {
            Target = target;
            MeshChange = change;
        }

        public override OpStatus Apply()
        {
            Target.EditAndUpdateMesh(
                (mesh) => { MeshChange.Apply(mesh); },
                GeometryEditTypes.VertexDeformation
            );
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            Target.EditAndUpdateMesh(
                (mesh) => { MeshChange.Revert(mesh); },
                GeometryEditTypes.VertexDeformation
            );
            return OpStatus.Success;
        }

        public override OpStatus Cull()
        {
            Target = null;
            MeshChange = null;
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
        CoordSpace space;

        public RepositionPivotChangeOp(Frame3f toPivot, DMeshSO target, CoordSpace space = CoordSpace.ObjectCoords)
        {
            Util.gDevAssert(space != CoordSpace.WorldCoords);       // not supported for now

            Target = target;
            toFrame = toPivot;
            this.space = space;
            if ( space == CoordSpace.ObjectCoords )
                initialFrame = Target.GetLocalFrame(CoordSpace.ObjectCoords);
            else 
                initialFrame = Target.GetLocalFrame(CoordSpace.SceneCoords);
        }

        public override OpStatus Apply()
        {
            if (space == CoordSpace.SceneCoords) {
                Frame3f localF = SceneTransforms.SceneToObject(Target, toFrame);
                Frame3f setF = Target.GetLocalFrame(CoordSpace.ObjectCoords).FromFrame(localF);
                Target.RepositionPivot(setF);
            } else {
                Target.RepositionPivot(toFrame);
            }
            return OpStatus.Success;
        }
        public override OpStatus Revert()
        {
            if (space == CoordSpace.SceneCoords) {
                Frame3f localF = SceneTransforms.SceneToObject(Target, initialFrame);
                Frame3f setF = Target.GetLocalFrame(CoordSpace.ObjectCoords).FromFrame(localF);
                Target.RepositionPivot(setF);
            } else {
                Target.RepositionPivot(initialFrame);
            }
            return OpStatus.Success;
        }

        public override OpStatus Cull()
        {
            Target = null;
            return OpStatus.Success;
        }
    }
}
