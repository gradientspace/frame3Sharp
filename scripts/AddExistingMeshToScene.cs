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
            BaseSceneConfig config = ContextSource.GetComponent<BaseSceneConfig>();
            if (config != null && config.Context != null && config.Context.Scene != null) {

                UnitySceneUtil.ImportExistingUnityMesh(this.gameObject, config.Context.Scene);

                Component.Destroy(this.gameObject.GetComponent<AddExistingMeshToScene>());
                if (DeleteOriginal)
                    this.gameObject.Destroy();
            }
        }
    }
}
