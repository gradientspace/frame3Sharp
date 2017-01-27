using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public class InputBehaviorSet
    {
        protected List<InputBehavior> Behaviors { get; set; }

        public InputBehaviorSet()
        {
            Behaviors = new List<InputBehavior>();
        }

        public void Add(InputBehavior behavior)
        {
            Behaviors.Add(behavior);
            Behaviors.Sort( (x, y) => x.Priority.CompareTo(y.Priority) );
        }
        public void Remove(InputBehavior behavior)
        {
            Behaviors.Remove(behavior);
            Behaviors.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }

        public void Add(InputBehaviorSet behaviors)
        {
            if (behaviors == null)
                return;
            foreach ( var b in behaviors.Behaviors)
                Behaviors.Add(b);
            Behaviors.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }
        public void Remove(InputBehaviorSet behaviors)
        {
            if (behaviors == null)
                return;
            foreach (var b in behaviors.Behaviors)
                Behaviors.Remove(b);
            Behaviors.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }


        // [RMS] maybe we should do capture & forward in this class, then
        //   we have more control? but what for?
        bool supports_input_type(InputBehavior b, InputState i) {
            return (b.SupportedDevices & i.eDevice) != 0;
        }


        public void CollectWantsCapture(InputState input, List<CaptureRequest> result)
        {
            foreach (InputBehavior b in Behaviors) {
                if (supports_input_type(b, input)) {
                    CaptureRequest req = b.WantsCapture(input);
                    if (req.type != CaptureRequestType.Ignore)
                        result.Add(req);
                }
            }
        }


        // this just calls UpdateBehavior on each behavior - these are special
        //   behaviors, should perhaps have a special type for them...
        public void SendOverrideInputs(InputState input)
        {
            CaptureData tmp = new CaptureData() { which = CaptureSide.Any };
            foreach (InputBehavior b in Behaviors)
                if (supports_input_type(b, input))
                    b.UpdateCapture(input, tmp);
        }



        public virtual void UpdateHover(InputState input)
        {
            foreach (InputBehavior b in Behaviors) {
                if (supports_input_type(b, input) == false)
                    continue;

                if (b.EnableHover)
                    b.UpdateHover(input);
            }
        }
        public virtual void EndHover(InputState input)
        {
            foreach (InputBehavior b in Behaviors) {
                if (supports_input_type(b, input) == false)
                    continue;

                if (b.EnableHover)
                    b.EndHover(input);
            }
        }


    }
}
