using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace f3
{
    public class AnimatedShowHideHUDItem : MonoBehaviour
    {
        public HUDStandardItem HUDItem { get; set; }
        public float FadeDuration { get; set; }
        public float ShowDuration { get; set; }
        public TweenCallback CompleteCallback { get; set; }

        public AnimatedShowHideHUDItem()
        {
            FadeDuration = 0.5f;
            ShowDuration = 3.0f;
        }

        public void Play()
        {
            HUDItem.Enabled = false;
            Fade = 0.0f;
            StartCoroutine(Animate());

        }
        public void Play(HUDStandardItem item, float showDuration = 3.0f, float fadeDuration = 0.5f)
        {
            HUDItem = item;
            ShowDuration = showDuration;
            FadeDuration = fadeDuration;
            Play();
        }

        public float Fade
        {
            get { return HUDItem.AlphaFade;  }
            set { HUDItem.AlphaFade = value; }
        }

        void set_enabled() {
            HUDItem.Enabled = true;
        }
        void set_disabled() {
            HUDItem.Enabled = false;
        }

        IEnumerator Animate()
        {
            yield return null;

            Sequence mySequence = DOTween.Sequence();
            mySequence.Append(
                DOTween.To(() => Fade, x => Fade = x, 1.0f, FadeDuration) );
            mySequence.AppendCallback(set_enabled);
            mySequence.AppendInterval(ShowDuration);
            mySequence.AppendCallback(set_disabled);
            mySequence.Append(
                DOTween.To(() => Fade, x => Fade = x, 0.0f, FadeDuration));
            if (CompleteCallback != null)
                mySequence.OnComplete(CompleteCallback);
        }
    }
}


