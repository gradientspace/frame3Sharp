using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    class SceneMeshImporter
    {
        public IOReadResult LastReadResult { get; set; }
        public bool SomeMeshesTooLargeForUnityWarning { get; set; }

        public struct ImportedObject
        {
            public UnityEngine.Mesh mesh;
            public SOMaterial material;      // may be null!
        }
        public List<ImportedObject> SceneObjects { get; set; }

        public bool SwapLeftRight { get; set; }
        public enum ImportMode
        {
            SeparateObjects,
            AsMeshReference
        }
        ImportMode ImportBehavior { get; set; }


        string sSourcePath;


        public SceneMeshImporter()
        {
            SwapLeftRight = true;       // this is right for importing from most other apps!
            ImportBehavior = ImportMode.AsMeshReference;
        }


        // parse file and create a set of MeshSO objects
        public bool ReadFile(string sPath)
        {
            sSourcePath = sPath;
            SomeMeshesTooLargeForUnityWarning = false;

            // read the input file

            SimpleMeshBuilder build = new SimpleMeshBuilder();

            StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = build };
            reader.warningEvent += on_warning;

            ReadOptions options = new ReadOptions();
            options.ReadMaterials = true;
            LastReadResult = reader.Read(sPath, options);
            if (LastReadResult.code != IOCode.Ok)
                return false;

            // create the material set

            List<SOMaterial> vSOMaterials = new List<SOMaterial>();
            for ( int k = 0; k < build.Materials.Count; ++k ) {
                SOMaterial m = build_material(sPath, build.Materials[k]);
                vSOMaterials.Add(m);
            }

            // convert the read meshes into unity meshes

            SceneObjects = new List<ImportedObject>();
            for ( int k = 0; k < build.Meshes.Count; ++k ) {
                if ( build.Meshes[k].VertexCount > 64000 || build.Meshes[k].TriangleCount > 64000 ) {
                    SomeMeshesTooLargeForUnityWarning = true;
                    continue;
                }

                int matID = build.MaterialAssignment[k];
                SOMaterial soMaterial = 
                    (matID < 0 || matID >= vSOMaterials.Count) ? null : vSOMaterials[matID];

                try {
                    Mesh unityMesh = UnityUtil.SimpleMeshToUnityMesh(build.Meshes[k], SwapLeftRight);
                    if (unityMesh != null)
                        SceneObjects.Add(new ImportedObject() { mesh = unityMesh, material = soMaterial });
                } catch (Exception e) {
                    Debug.Log("[UnitySceneImporter] error converting to unity mesh : " + e.Message);
                }
            }

            return SceneObjects.Count > 0;
        }

        private void on_warning(string message, object extra_data)
        {
            Debug.Log("[SimpleMeshReader/Warning] " + message);
        }

 
        // add the meshes we have read into SceneObjects list to the given Scene
        public void AppendReadMeshesToScene(FScene scene, bool bSaveHistory)
        {
            if (ImportBehavior == ImportMode.AsMeshReference) {
                MeshReferenceSO ref_group = GetMeshReference(scene.DefaultMeshSOMaterial);

                var change = new AddSOChange() { scene = scene, so = ref_group };
                if (bSaveHistory)
                    scene.History.PushChange(change);
                else
                    change.Apply();

            } else {
                foreach (ImportedObject obj in SceneObjects) {
                    MeshSO meshSO = new MeshSO();
                    meshSO.Create(obj.mesh,
                        (obj.material == null) ? scene.DefaultMeshSOMaterial : obj.material);
                    meshSO.Name = UniqueNames.GetNext("ImportMesh");

                    var change = new AddSOChange() { scene = scene, so = meshSO };
                    if (bSaveHistory)
                        scene.History.PushChange(change);
                    else
                        change.Apply();
                }
            }
        }


        public MeshReferenceSO GetMeshReference(SOMaterial defaultMaterial)
        {
            MeshReferenceSO ref_group = new MeshReferenceSO();
            ref_group.MeshReferencePath = sSourcePath;
            ref_group.Create();
            ref_group.Name = UniqueNames.GetNext("MeshReference");

            foreach (ImportedObject obj in SceneObjects) {
                MeshSO meshSO = new MeshSO();
                meshSO.Create(obj.mesh,
                    (obj.material == null) ? defaultMaterial : obj.material);
                meshSO.Name = UniqueNames.GetNext("ImportMesh");
                ref_group.AddChild(meshSO);
            }

            return ref_group;
        }





        // https://en.wikipedia.org/wiki/Wavefront_.obj_file#Material_template_library
        SOMaterial build_material(string sSourceFilePath, g3.GenericMaterial mIn)
        {
            OBJMaterial objMat = mIn as OBJMaterial;
            // TODO handle other types?

            if (objMat.illum < 0 || objMat.illum == 1 || objMat.illum == 2) {
                SOMaterial sceneMat = new SOMaterial() {
                    Name = objMat.name,
                    Type = SOMaterial.MaterialType.TextureMap,
                    RGBColor = toColor(objMat.DiffuseColor, objMat.Alpha)
                };

                if (objMat.map_Kd != null && objMat.map_Kd != "") {
                    Texture2D tex = load_texture(sSourceFilePath, objMat.map_Kd);
                    sceneMat.MainTexture = tex;
                    if (objMat.Kd == GenericMaterial.Invalid)
                        sceneMat.RGBColor = Colorf.White;
                }
                return sceneMat;
            }

            return null;
        }


        Texture2D load_texture(string sSourceFilePath, string sMaterialPath)
        {
            string sFullpath = Path.Combine(Path.GetDirectoryName(sSourceFilePath), sMaterialPath);
            if (!File.Exists(sFullpath)) {
                Debug.Log("[SceneImporter] cannot find map image " + sFullpath);
                return null;
            }

            Texture2D tex;
            try {
                var bytes = File.ReadAllBytes(sFullpath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(bytes);
            } catch (Exception e) {
                Debug.Log("[SceneImporter] exception loading texure data from " + sFullpath + " : " + e.Message);
                return null;
            }
            return tex;
        }



        public static Colorf toColor(Vector3f v, float alpha = 1.0f)
        {
            return new Colorf(v[0], v[1], v[2], alpha);
        }




    }
}
