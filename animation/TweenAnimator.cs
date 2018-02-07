using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace f3
{
    public class TweenAnimator : IAnimatable
    {
        bool deregister = false;

        float start_time;


        public float Duration = 1.0f;

        /// <summary>
        /// You must replace this function, or this object is useless
        /// </summary>
        public Action<float> tweenF = (t) => { };


        public Action OnCompletedF = null;


        public TweenAnimator() { }
        public TweenAnimator(Action<float> tweenF, float duration)
        {
            this.tweenF = tweenF;
            Duration = duration;
        }


        public virtual void Start()
        {
            start_time = FPlatform.RealTime();
        }


        public virtual void Update()
        {
            if (deregister == true)
                return;

            float dt = FPlatform.RealTime() - start_time;

            if ( dt >= Duration ) {
                dt = Duration;
                deregister = true;
            }

            tweenF(dt / Duration);

            if (deregister && OnCompletedF != null)
                OnCompletedF();
        }


        // if you return true, this animation is removed
        public bool DeregisterNextFrame {
            get { return deregister; }
        }
    }
}
