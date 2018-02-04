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
            if (tagsIn != null)
                tags = new List<string>(tagsIn);
            else
                tags = new List<string>();
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
            new SOType("SphereSO", Type.GetType("f3.SphereSO"), SOType.TagPrimitive);

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

        static readonly public SOType DMesh =
            new SOType("DMeshSO", Type.GetType("f3.DMeshSO"));

        static readonly public SOType MeshReference =
            new SOType("MeshReferenceSO", Type.GetType("f3.MeshReferenceSO"));

    }



    //
    // SOType registry, which allows clients to register SOTypes that can be saved/loaded
    //
    public class SORegistry
    {
        struct SOInfo
        {
            public SOType type;
            public SOEmitSerializationFunc serializer;
            public SOBuildFunc builder;
        }

        Dictionary<string, SOInfo> knownTypes;

        public SORegistry()
        {
            knownTypes = new Dictionary<string, SOInfo>();
        }


        public void RegisterType(SOType t, SOEmitSerializationFunc serializeFunc = null,
                                 SOBuildFunc buildFunc = null)
        {
            if (knownTypes.ContainsKey(t.identifier))
                throw new InvalidOperationException("SORegistry.RegisterType: type " + t.identifier + " already registered!");

            SOInfo info = new SOInfo() {
                type = t,
                serializer = serializeFunc,
                builder = buildFunc
            };
            knownTypes[t.identifier] = info;
        }


        public SOType FindType(string typeIdentifier)
        {
            if (knownTypes.ContainsKey(typeIdentifier) == false)
                return SOTypes.Unknown;
            return knownTypes[typeIdentifier].type;
        }

        public SOEmitSerializationFunc FindSerializer(string typeIdentifier)
        {
            if (knownTypes.ContainsKey(typeIdentifier) == false)
                return null;
            return knownTypes[typeIdentifier].serializer;
        }

        public SOBuildFunc FindBuilder(string typeIdentifier)
        {
            if (knownTypes.ContainsKey(typeIdentifier) == false)
                return null;
            return knownTypes[typeIdentifier].builder;
        }

        public bool ContainsType(string typeIdentifier)
        {
            return knownTypes.ContainsKey(typeIdentifier);
        }


    }
}
