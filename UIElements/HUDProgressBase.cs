using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public class HUDProgressBase : HUDStandardItem
    {
        protected fGameObject rootGO;

        public virtual void Create()
        {
            rootGO = new GameObject(UniqueNames.GetNext("HUDProgress"));
        }

        public event ValueChangedHandler OnProgressChanged;
        public event EndValueChangeHandler OnProgressCompleted;

        double current_progress;

        double max_progress = 1.0;

        public double Progress {
            get { return current_progress; }
            set {
                if (current_progress != value)
                    update_progress(value, true);
            }
        }

        public double MaxProgress {
            get { return max_progress; }
            set {
                if ( max_progress != value ) {
                    max_progress = value;
                    update_progress(current_progress, true, true);
                }
            }
        }


        void update_progress(double newValue, bool bSendEvent, bool max_changed = false)
        {
            if (newValue != current_progress || max_changed) {
                double prev = current_progress;
                current_progress = newValue;
                update_geometry();
                if (bSendEvent) {
                    FUtil.SafeSendEvent(OnProgressChanged, this, prev, current_progress);
                    if ( current_progress == max_progress )
                        FUtil.SafeSendEvent(OnProgressCompleted, this, max_progress);

                }
            }
        }


        protected virtual void update_geometry()
        {
            // subclass should override this to be notified that it is time to update internals
        }



		#region SceneUIElement implementation

		override public fGameObject RootGameObject {
			get { return rootGO; }
		}

        override public bool WantsCapture(InputEvent e)
        {
            return false;
        }

        override public bool BeginCapture (InputEvent e)
		{
            return false;
		}

		override public bool UpdateCapture (InputEvent e)
		{
            return false;
		}

		override public bool EndCapture (InputEvent e)
		{
			return false;
		}

		#endregion
    }
}
