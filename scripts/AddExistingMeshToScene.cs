using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    class AddExistingMeshToScene : MonoBehaviour
    {
        public GameObject ContextSource = null;
        public bool DeleteOriginal = true;

        void Update()
        {
            if (ContextSource == null)
                return;

            MeshFilter meshF = this.gameObject.GetComponent<MeshFilter>();
            if (meshF == null)
                throw new Exception("AddExistingMeshToScene.Update: parent gameObject is not a mesh!!");

            BaseSceneConfig config = ContextSource.GetComponent<BaseSceneConfig>();
            if (config != null && config.Context != null && config.Context.Scene != null) {

                MeshSO newSO  = new MeshSO();
                newSO.Create(meshF.mesh, config.Context.Scene.DefaultMeshSOMaterial);

                Frame3f frameW = UnityUtil.GetGameObjectFrame(this.gameObject, CoordSpace.WorldCoords);
                newSO.SetLocalFrame(frameW, CoordSpace.WorldCoords);

                config.Context.Scene.AddSceneObject(newSO, true);

                Component.Destroy(this.gameObject.GetComponent<AddExistingMeshToScene>());
                if (DeleteOriginal)
                    GameObject.Destroy(this.gameObject);
            }
        }
    }
}
