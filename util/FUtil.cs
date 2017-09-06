using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public static class FUtil
    {

        // [RMS] this is more expensive than a specific delegate
        public static void SafeSendAnyEvent(Delegate d, params object[] args) {
            var tmp = d;
            if (tmp != null)
                tmp.DynamicInvoke(args);
        }


        public static void SafeSendEvent(EventHandler handler, object sender, EventArgs e) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, e);
        }

        public static void SafeSendEvent(SceneSelectionChangedHandler handler, object sender, EventArgs e) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, e);
        }


        public static void SafeSendEvent(InputEventHandler handler, object sender, InputEvent e) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, e);
        }

        public static void SafeSendEvent(BeginValueChangeHandler handler, object sender, double startValue) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, startValue);
        }
        public static void SafeSendEvent(ValueChangedHandler handler, object sender, double oldValue, double newValue) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, oldValue, newValue);
        }
        public static void SafeSendEvent(EndValueChangeHandler handler, object sender, double endValue) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, endValue);
        }

        public static void SafeSendEvent(TextChangedHander handler, object sender, string newText) {
            var tmp = handler;
            if (tmp != null)
                tmp(sender, newText);
        }

        public static void SafeSendEvent(EditStateChangeHandler handler, object source) {
            var tmp = handler;
            if (tmp != null)
                tmp(source);
        }

        public static void SafeSendEvent(BoundsModifiedEventHandler handler, object source)
        {
            var tmp = handler;
            if (tmp != null)
                tmp(source);
        }
            

        public static void SafeSendEvent(OnChangeOpHandler handler, object source, IChangeOp change) {
            var tmp = handler;
            if (tmp != null)
                tmp(source, change);
        }
    }
}
