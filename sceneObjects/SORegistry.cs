using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{

    //
    // SOType identifies different SceneObject types. Used internally in various
    //  ways, to be a bit more structured than working with raw C# types
    public struct SOType
    {
        public readonly string identifier;
        public readonly Type type;
        public readonly List<string> tags;

        public SOType(string id, Type t)
        {
            identifier = id;
            type = t;
            tags = new List<string>();
        }
        public SOType(string id, Type t, params string[] tagsIn)
        {
            identifier = id;
            type = t;
            tags = new List<string>(tagsIn);
        }

        public bool hasTag(string tag) {
            return tags.Contains(tag);
        }

        public const string TagPrimitive = "primitive";



        static public bool operator ==(SOType t1, SOType t2) { return t1.identifier == t2.identifier; }
        static public bool operator !=(SOType t1, SOType t2) { return t1.identifier != t2.identifier; }
        public override bool Equals(object obj) { return this.identifier == ((SOType)obj).identifier; }
        public override int GetHashCode() { return identifier.GetHashCode(); }
    }


    //
    // Built-in SOTypes included with frame3
    //
    static public class SOTypes
    {
        static readonly public SOType Unknown =
            new SOType("unknown", null);

        static readonly public SOType Group =
            new SOType("GroupSO", Type.GetType("f3.GroupSO"));


        // Primitives

        static readonly public SOType Cylinder = 
            new SOType("CylinderSO", Type.GetType("f3.CylinderSO"), SOType.TagPrimitive);

        static readonly public SOType Box = 
            new SOType("BoxSO", Type.GetType("f3.BoxSO"), SOType.TagPrimitive);

        static readonly public SOType Sphere = 
            new SOType("SphereSO", Type.GetType("f3.BoxSO"), SOType.TagPrimitive);

        static readonly public SOType Pivot =
            new SOType("PivotSO", Type.GetType("f3.PivotSO") );


        // Curves

        static readonly public SOType PolyCurve =
            new SOType("PolyCurveSO", Type.GetType("f3.PolyCurveSO"));

        static readonly public SOType PolyTube =
            new SOType("PolyTubeSO", Type.GetType("f3.PolyTubeSO"));


        // Meshes

        static readonly public SOType Mesh =
            new SOType("MeshSO", Type.GetType("f3.MeshSO"));

        static readonly public SOType MeshReference =
            new SOType("MeshReferenceSO", Type.GetType("f3.MeshReferenceSO"));

    }


    //
    // SOType registry. Currently not used! but it will be
    //
    public class SORegistry
    {
        Dictionary<string, SOType> knownTypes;

        public SORegistry()
        {
            knownTypes = new Dictionary<string, SOType>();
        }


        public void RegisterType(SOType t)
        {
            if (knownTypes.ContainsKey(t.identifier))
                throw new InvalidOperationException("SORegistry.RegisterType: type " + t.identifier + " already registered!");

            knownTypes[t.identifier] = t;
        }


        public SOType FindType(string identifier)
        {
            if (knownTypes.ContainsKey(identifier) == false)
                return SOTypes.Unknown;

            return knownTypes[identifier];
        }

    }
}
