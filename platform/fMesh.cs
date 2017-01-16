using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public class fMesh
    {
        Mesh unitymesh;

        public fMesh(UnityEngine.Mesh m)
        {
            unitymesh = m;
        }

        public fMesh Clone()
        {
            return new fMesh(Mesh.Instantiate(unitymesh));
        }


        public static implicit operator UnityEngine.Mesh(fMesh mesh)
        {
            return mesh.unitymesh;
        }
    }
}
