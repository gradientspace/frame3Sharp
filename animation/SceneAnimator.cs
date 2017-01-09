using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DG.Tweening;

namespace f3
{
    public class SceneAnimator : MonoBehaviour
    {
        public FScene Scene { get; set; } 

        //public void Play



        public double CurrentTime
        {
            get { return Scene.CurrentTime; }
            set { Scene.CurrentTime = value; }
        }



        public void PlayFromCurrent(double fToTime, float fDuration)
        {
            StartCoroutine(playFromCurrent(fToTime, fDuration));
        }


        IEnumerator playFromCurrent(double fToTime, float fDuration)
        {
            yield return null;

            Tweener tween = DOTween.To(() => CurrentTime, x => CurrentTime = x, fToTime, fDuration);
            //if (CompleteCallback != null)
            //    tween.OnComplete(CompleteCallback);
        }

    }
}
