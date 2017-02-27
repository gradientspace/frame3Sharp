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


    }
}
