using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public static class UnitySceneUtil
    {

        // converts input GameObject into a DMeshSO. Original GameObject is not used by DMeshSO,
        // so it can be destroyed.
        // [TODO] transfer materials!!
        public static void WrapMeshGameObject(GameObject wrapGO, FContext context, bool bDestroyOriginal)
        {
            var wrapperSO = ImportExistingUnityMesh(wrapGO, context.Scene, true, true, 
                        (mesh, material) => {
                            DMeshSO gso = new DMeshSO();
                            return gso.Create(UnityUtil.UnityMeshToDMesh(mesh, false), material);
                        } );
            if ( bDestroyOriginal )
                GameObject.Destroy(wrapGO);
            context.Scene.AddSceneObject(wrapperSO, true);
        }



        // embeds input GameObject in a GOWrapperSO, which provides some basic
        // F3 functionality for arbitrary game objects
        public static void WrapAnyGameObject(GameObject wrapGO, FContext context, bool bAllowMaterialChanges)
        {
            GOWrapperSO wrapperSO = new GOWrapperSO() {
                AllowMaterialChanges = bAllowMaterialChanges
            };
            wrapperSO.Create(wrapGO);
            context.Scene.AddSceneObject(wrapperSO, true);
        }




        // extracts MeshFilter object from input GameObject and passes it to a custom constructor
        // function MakeSOFunc (if null, creates basic MeshSO). Then optionally adds to Scene,
        // preserving existing 3D position if desired (default true)
        public static TransformableSO ImportExistingUnityMesh(GameObject go, FScene scene, 
            bool bAddToScene = true, bool bKeepWorldPosition = true,
            Func<Mesh, SOMaterial, TransformableSO> MakeSOFunc = null )
        {
            MeshFilter meshF = go.GetComponent<MeshFilter>();
            if (meshF == null)
                throw new Exception("SceneUtil.ImportExistingUnityMesh: gameObject is not a mesh!!");

            Mesh useMesh = meshF.mesh;
            AxisAlignedBox3f bounds = useMesh.bounds;
            UnityUtil.TranslateMesh(useMesh, -bounds.Center.x, -bounds.Center.y, -bounds.Center.z);
            useMesh.RecalculateBounds();

            TransformableSO newSO = (MakeSOFunc != null) ? 
                MakeSOFunc(useMesh, scene.DefaultMeshSOMaterial)
                : new MeshSO().Create(useMesh, scene.DefaultMeshSOMaterial);

            if ( bAddToScene )
                scene.AddSceneObject(newSO, false);

            if ( bKeepWorldPosition ) {
                Frame3f goFrameW = UnityUtil.GetGameObjectFrame(go, CoordSpace.WorldCoords);
                Frame3f goFrameS = scene.ToSceneFrame(goFrameW);
                Vector3f boundsCenterS = scene.ToSceneP(goFrameW.Origin + bounds.Center);

                // translate to position in scene
                Frame3f curF = newSO.GetLocalFrame(CoordSpace.SceneCoords);
                curF.Origin += boundsCenterS;
                newSO.SetLocalFrame(curF, CoordSpace.SceneCoords);

                curF = newSO.GetLocalFrame(CoordSpace.SceneCoords);
                curF.RotateAround(Vector3.zero, goFrameS.Rotation);
                newSO.SetLocalFrame(curF, CoordSpace.SceneCoords);
            }

            return newSO;
        }


    }




}
