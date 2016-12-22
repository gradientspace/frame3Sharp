using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    // imports an existing GO into scene xform hierarchy, as a GOWrapperSO.
    class AddExistingGOToScene : MonoBehaviour
    {
        public GameObject ContextSource = null;

        void Update()
        {
            if (ContextSource == null)
                return;

            BaseSceneConfig config = ContextSource.GetComponent<BaseSceneConfig>();
            if ( config != null && config.Context != null && config.Context.Scene != null ) {

                GOWrapperSO newSO = new GOWrapperSO();
                newSO.Create(this.gameObject);

                config.Context.Scene.AddSceneObject(newSO);

                Component.Destroy(this.gameObject.GetComponent<AddExistingGOToScene>());
            }
        }
       

    }
}
