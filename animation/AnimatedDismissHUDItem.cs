using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace f3
{
    public class AnimatedDismissHUDItem : MonoBehaviour
    {
        public HUDStandardItem HUDItem { get; set; }
        public float Duration { get; set; }
        public TweenCallback CompleteCallback { get; set; }

        public AnimatedDismissHUDItem()
        {
            Duration = 0.5f;
        }

        public void Play()
        {
            HUDItem.Enabled = false;
            StartCoroutine(Animate());

        }
        public void Play(HUDStandardItem item, float duration = 0.5f)
        {
            HUDItem = item;
            Duration = duration;
            Play();
        }

        public float Fade
        {
            get { return HUDItem.AlphaFade;  }
            set { HUDItem.AlphaFade = value; }
        }

        IEnumerator Animate()
        {
            yield return null;

            Tweener tween = DOTween.To(() => Fade, x => Fade = x, 0.0f, Duration);
            if (CompleteCallback != null)
                tween.OnComplete(CompleteCallback);
        }
    }
}


