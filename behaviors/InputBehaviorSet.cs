using System;
using System.Collections;
using System.Collections.Generic;

namespace f3
{
    public interface InputBehaviorSource
    {
        InputBehaviorSet InputBehaviors { get; }
    }



    public class InputBehaviorSet : IEnumerable<InputBehavior>
    {
        protected struct BehaviorInfo
        {
            public InputBehavior b;
            public object source;
            public string group; 
        } 

        protected List<BehaviorInfo> Behaviors { get; set; }

        public object DefaultSource = null;

        public InputBehaviorSet()
        {
            Behaviors = new List<BehaviorInfo>();
        }


        public delegate void SetChangedHandler(InputBehaviorSet set);
        public SetChangedHandler OnSetChanged;


        public int Count {
            get { return Behaviors.Count; }
        }


        public void Add(InputBehavior behavior, object source = null, string group = "")
        {
            if (source == null)
                source = DefaultSource;
            Behaviors.Add(new BehaviorInfo() { b = behavior, source = source, group = group });
            behaviors_modified();
        }
        public void Remove(InputBehavior behavior)
        {
            int idx = Behaviors.FindIndex((x) => { return x.b == behavior; });
            if (idx >= 0) {
                Behaviors.RemoveAt(idx);
                behaviors_modified();
            }
        }

        public void Add(InputBehaviorSet behaviors, string new_group = "" )
        {
            if (behaviors == null)
                return;
            foreach (BehaviorInfo b in behaviors.Behaviors) {
                BehaviorInfo bcopy = b;
                if ( new_group != "" )
                    bcopy.group = new_group;
                Behaviors.Add(bcopy);
            }
            behaviors_modified();
        }
        public void Remove(InputBehaviorSet behaviors)
        {
            if (behaviors == null)
                return;
            foreach (var binfo in behaviors.Behaviors) {
                int idx = Behaviors.FindIndex((x) => { return x.b == binfo.b; });
                Behaviors.RemoveAt(idx);
            }
            behaviors_modified();
        }

        public List<InputBehavior> RemoveByGroup(string group)
        {
            List<InputBehavior> removed = new List<InputBehavior>();
            for ( int i = 0; i < Behaviors.Count; ++i ) {
                if ( Behaviors[i].group == group ) {
                    removed.Add(Behaviors[i].b);
                    Behaviors.RemoveAt(i);
                    i--;
                }
            }
            behaviors_modified();
            return removed;
        }

        void behaviors_modified()
        {
            Behaviors.Sort((x, y) => x.b.Priority.CompareTo(y.b.Priority));
            FUtil.SafeSendAnyEvent(OnSetChanged, this);
        }

        // [RMS] maybe we should do capture & forward in this class, then
        //   we have more control? but what for?
        bool supports_input_type(InputBehavior b, InputState i) {
            return (b.SupportedDevices & i.eDevice) != 0;
        }


        public void CollectWantsCapture(InputState input, List<CaptureRequest> result)
        {
            foreach (BehaviorInfo binfo in Behaviors) {
                if (supports_input_type(binfo.b, input)) {
                    CaptureRequest req = binfo.b.WantsCapture(input);
                    if (req.type != CaptureRequestType.Ignore)
                        result.Add(req);
                }
            }
        }


        // this just calls UpdateCapture on each behavior - these are special
        //   behaviors, should perhaps have a special type for them?
        // If UpdateCapture returns State.End, we assume it wanted to "consume"
        //   the event, and halt the iteration.
        public void SendOverrideInputs(InputState input)
        {
            CaptureData tmp = new CaptureData() { which = CaptureSide.Any };
            foreach (BehaviorInfo b in Behaviors) {
                if (supports_input_type(b.b, input)) {
                    Capture result = b.b.UpdateCapture(input, tmp);
                    if (result.state == CaptureState.End)
                        break;     // consume this event
                }
            }
        }



        public virtual void UpdateHover(InputState input)
        {
            foreach (BehaviorInfo b in Behaviors) {
                if (supports_input_type(b.b, input) == false)
                    continue;

                if (b.b.EnableHover)
                    b.b.UpdateHover(input);
            }
        }
        public virtual void EndHover(InputState input)
        {
            foreach (BehaviorInfo b in Behaviors) {
                if (supports_input_type(b.b, input) == false)
                    continue;

                if (b.b.EnableHover)
                    b.b.EndHover(input);
            }
        }



        public IEnumerator<InputBehavior> GetEnumerator()
        {
            for (int k = 0; k < Behaviors.Count; ++k)
                yield return Behaviors[k].b;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }



    }
}
