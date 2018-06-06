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
            if (mesh == null) {
                DebugUtil.Log(2, "[FResources.LoadMesh] mesh not found at {0}", sPath);
                return new fMesh(new g3.DMesh3());
            }
            return new fMesh(mesh);
        }


        public static MemoryStream LoadBinary(string sPath)
        {
            TextAsset asset = Resources.Load(sPath) as TextAsset;
            if (asset == null) {
                DebugUtil.Log(2, "[FResources.LoadBinary] file not found at {0}", sPath);
                return null;
            }
            return new MemoryStream(asset.bytes);
        }


        public static string LoadText(string sPath)
        {
            TextAsset asset = Resources.Load(sPath) as TextAsset;
            if (asset == null) {
                DebugUtil.Log(2, "[FResources.LoadText] file not found at {0}", sPath);
                return "";
            }
            return asset.text;
        }

    }
}
