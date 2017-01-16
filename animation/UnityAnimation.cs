using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public class UnityPerFrameAnimationBehavior : MonoBehaviour
    {
        public IPerFrameAnimator Animator;

        public void Update()
        {
            Animator.NextFrame();
        }
    }
}
