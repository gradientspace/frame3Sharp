using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public abstract class BaseSceneConfig : MonoBehaviour, FPlatform.CoroutineExecutor
    {
        public abstract FContext Context { get; }


        public bool AutoConfigVR = false;

        public BaseSceneConfig()
        {
        }

        public virtual void Awake()
        {
            FPlatform.CoroutineExec = this;
        }

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
            // background threads should watch for this and suicide if it becomes true
            FPlatform.ShutdownBackgroundThreadsOnQuit = true;
        }

        /*
         * FPlatform.CoroutineExecutor impl
         */
        public virtual void StartAnonymousCoroutine(IEnumerator func)
        {
            if (func != null)
                StartCoroutine(func);
            else
                DebugUtil.Log("BaseSceneConfig.StartAnonymousCoroutine: tried to start null enumerator");
        }

    }
}
