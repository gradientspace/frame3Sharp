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
            public DMesh3 mesh;
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
            ImportBehavior = ImportMode.SeparateObjects;
        }


        // parse file and create a set of MeshSO objects
        public bool ReadFile(string sPath)
        {
            sSourcePath = sPath;
            SomeMeshesTooLargeForUnityWarning = false;

            // read the input file

            DMesh3Builder build = new DMesh3Builder();

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
                DMesh3 mesh = build.Meshes[k];

                int matID = build.MaterialAssignment[k];
                SOMaterial soMaterial = 
                    (matID < 0 || matID >= vSOMaterials.Count) ? null : vSOMaterials[matID];

                if (SwapLeftRight)
                    MeshTransforms.FlipLeftRightCoordSystems(mesh);

                SceneObjects.Add(new ImportedObject() { mesh = mesh, material = soMaterial });
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
                    DMeshSO meshSO = new DMeshSO();
                    meshSO.Create(obj.mesh, (obj.material == null) ? scene.DefaultMeshSOMaterial : obj.material);
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
                DMeshSO meshSO = new DMeshSO();
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

            int lightmodel = objMat.illum;
            if (lightmodel > 2)     // [RMS] we don't support these anyway, but don't want to ignore these materials...
                lightmodel = 2;
            if (lightmodel < 0 || lightmodel == 0 || lightmodel == 1 || lightmodel == 2) {
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
