using System;
using System.Collections.Generic;
using System.Timers;
using g3;

namespace f3
{
    public class HUDToast : HUDLabel
    {
        bool hovering = false;
        bool dismiss = false;
        bool disappear = false;
        Timer dismiss_timer = null;


        /// <summary>
        /// By default the Toast will not fade out if the user is hovering on it. 
        /// Set this to false to disable this behavior.
        /// </summary>
        public bool HoldOnHover = true;


        /// <summary>
        /// If enabled, then toast will be dismissed if clicked. Must be set
        /// before calling Show()
        /// </summary>
        public bool ClickToDismiss = false;


        /// <summary>
        /// Called when Toast is dismissed
        /// </summary>
        public EventHandler OnDismissed;


        /// <summary>
        /// Show the Toast widget with an animated fade in. The widget will fade out 
        /// after Duration seconds, unless the user hovers over it, in which case it
        /// will stay until they end the hover.
        /// </summary>
        public void Show(Cockpit cockpit, float fDuration, float fFadeInOut = 0.25f)
        {
            AlphaFade = 0.0f;

            hovering = false;
            disappear = false;

            // if this goes true, toast will begin animated-fade on next frame
            dismiss = false;

            // we register a basic animator that just watches for dismiss=true and
            // when that happens we fade out and remove the toast
            cockpit.HUDAnimator.Register(
                new GenericAnimatable(() => {
                    if (dismiss) {
                        FUtil.SafeSendEvent(this.OnDismissed, this, EventArgs.Empty);
                        HUDUtil.AnimatedDimiss_Cockpit(this, cockpit, true, (disappear) ? 0.001f : fFadeInOut );
                        dismiss_timer.Dispose();
                        return true;
                    }
                    return false;
                })
            );


            // timer event that is called after the initial duration, and then ever
            // couple hundred MS to check that hover ended.
            // This is called from a Timer, which runs in a separate thread, and hence
            // we cannot manipulate the scene in this function
            // TODO: couldn't we just do this in animator above??
            Action timer_elapsed = () => {
                if (hovering) {
                    dismiss_timer.Interval = 300;
                    dismiss_timer.Start();
                } else
                    dismiss = true;
            };


            // ok, this kicks everything off - we do animated transition to show ourself,
            // and then start a timer that waits the initial duration
            HUDUtil.AnimatedShow(this, fFadeInOut, () => {
                dismiss_timer = new Timer(fDuration*1000);
                dismiss_timer.Elapsed += (o, e) => {
                    timer_elapsed();
                };
                dismiss_timer.Enabled = true;
            });


            if ( ClickToDismiss ) {
                this.OnClicked += (s, e) => {
                    dismiss = true;
                };
            }


        }


        /// <summary>
        /// Force fade-out of the toast. If bImmediate is true, it will
        /// disappear without a fade transition.
        /// </summary>
        public virtual void Dismiss(bool bImmediate = false)
        {
            dismiss = true;
            disappear = bImmediate;
        }



        protected override void OnEnabledChanged()
        {
            //base.OnEnabledChanged();
            //UpdateText();
        }



        override public bool EnableHover {
            get { return HoldOnHover; }
        }
        override public void UpdateHover(Ray3f ray, UIRayHit hit)
        {
            hovering = true;
        }
        override public void EndHover(Ray3f ray)
        {
            hovering = false;
        }

    }
}
