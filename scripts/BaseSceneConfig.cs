using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public abstract class BaseSceneConfig : MonoBehaviour
    {
        public abstract FContext Context { get; }


        public bool AutoConfigVR = false;


        // Update is called once per frame
        public virtual void Update()
        {
            Context.Update();
        }


        public virtual void OnApplicationFocus( bool hasFocus )
        {
            Context.OnFocusChange(hasFocus);
        }


        public virtual void OnApplicationQuit()
        {
            // let client decide what to do here
        }


    }
}
