using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public static class FResources
    {
        public static fMesh LoadMesh(string sPath)
        {
            Mesh mesh = Resources.Load<Mesh>(sPath);
            return new fMesh(mesh);
        }
    }
}
