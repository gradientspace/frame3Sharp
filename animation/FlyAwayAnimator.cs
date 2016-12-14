using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace f3
{
    public class FlyAwayAnimator : MonoBehaviour
    {
        public HUDStandardItem HUDItem { get; set; }
        public Vector3 Direction { get; set; }
        public float Distance { get; set; }

        public float Duration { get; set; }

        public TweenCallback CompleteCallback { get; set; }

        public FlyAwayAnimator()
        {
            Direction = Vector3.right;
            Distance = 1.0f;
            Duration = 1.0f;
        }

        public void Begin(HUDStandardItem item, float duration = 1.0f)
        {
            this.HUDItem = item;
            this.Duration = duration;
            StartCoroutine(Animate());
        }


        public float Fade
        {
            get { return HUDItem.AlphaFade; }
            set { HUDItem.AlphaFade = value; }
        }

        IEnumerator Animate()
        {
            yield return null;

            Vector3 endPoint = this.gameObject.transform.position + Distance * Direction;
            Tweener moveTween = this.gameObject.transform.DOMove(endPoint, Duration);
            // [RMS] fade doesn't work on most objects because of shaders
            //DOTween.To(() => Fade, x => Fade = x, 0.0f, Duration);
            if (CompleteCallback != null)
                moveTween.OnComplete(CompleteCallback);
        }
    }
}
