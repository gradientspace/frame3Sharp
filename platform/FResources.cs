using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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


        public static MemoryStream LoadBinary(string sPath)
        {
            TextAsset asset = Resources.Load(sPath) as TextAsset;
            return new MemoryStream(asset.bytes);
        }

    }
}
