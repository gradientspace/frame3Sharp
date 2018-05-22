using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using f3;
using DG.Tweening;

namespace f3
{
    /// <summary>
    /// This is a base class that provides some basic functionality to use unityUI Panels as 
    /// dialogs. Currently just does fading hide/show transitions.
    /// </summary>
    public class UnityUIDialogBase : MonoBehaviour
    {
        public float TransitionInSpeed = 0.4f;
        public float TransitionOutSpeed = 0.6f;

        public virtual bool HideOnAwake {
            get { return true; }
        }

        public virtual void TransitionVisibility(bool bVisible)
        {
            if (bVisible) {
                fade_transition(true);
                on_show();
            } else {
                on_hide();
                fade_transition(false);
            }
        }


        public virtual void Awake()
        {
            if (HideOnAwake)
                this.gameObject.GetComponent<CanvasGroup>().alpha = 0;
        }


        protected virtual void fade_transition(bool bShow)
        {
            var cg = this.gameObject.GetComponent<CanvasGroup>();

            bool bVisible = this.gameObject.IsVisible();
            if ((bVisible && bShow && cg.alpha == 1) || (bVisible == false && bShow == false))
                return;

            if (bShow) {
                cg.alpha = 0;
                this.gameObject.SetVisible(true);
                DOTween.To(() => { return cg.alpha; }, x => { cg.alpha = x; }, 1.0f, TransitionInSpeed);
            } else {
                DOTween.To(() => { return cg.alpha; }, x => { cg.alpha = x; }, 0.0f, TransitionOutSpeed)
                    .OnComplete(
                        () => { on_hide_transition_complete(); });
            }
        }


        protected virtual void on_show()
        {
        }
        protected virtual void on_hide()
        {
        }

        protected virtual void on_hide_transition_complete()
        {
            this.gameObject.Hide();
        }


    }
}
