using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    public enum fPrimitiveType
    {
        Disc,
        Box
    }

    public static class PrimitiveCache
    {
        private static Dictionary<fPrimitiveType, fMesh> Cache = new Dictionary<fPrimitiveType, fMesh>();

        public static fMesh GetPrimitiveMesh(fPrimitiveType eType)
        {
            if (!Cache.ContainsKey(eType)) {
                switch (eType) {
                    case fPrimitiveType.Disc:
                        Cache[eType] = MakeDisc();
                        break;
                    case fPrimitiveType.Box:
                        Cache[eType] = MakeBox();
                        break;
                    default:
                        throw new Exception("PrimitiveCache.GetPrimitiveMesh: type " + eType.ToString() + " is not implemented!");
                }
            }
            return Cache[eType].Clone();
        }



        static fMesh MakeDisc()
        {
            TrivialDiscGenerator gen = new TrivialDiscGenerator() {
                Clockwise = false
            };
            gen.Generate();
            return new fMesh(gen.MakeUnityMesh());
        }

        static fMesh MakeBox()
        {
            TrivialBox3Generator gen = new TrivialBox3Generator() {
                Clockwise = false,
                NoSharedVertices = true
            };
            gen.Generate();
            return new fMesh(gen.MakeUnityMesh());
        }

    }
}
