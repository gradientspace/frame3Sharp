using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    // You can subclass Keyframe and implement your own constructor to add custom attributes
    // [TODO] add generic attribute list?
    public class Keyframe
    {
        public readonly double Time;
        public readonly Frame3f Frame;

        public Keyframe(double time, Frame3f frame) {
            Time = time;
            Frame = frame;
        }

        public Keyframe(Keyframe parent1, Keyframe parent2, double interpTime)
        {
            Time = interpTime;

            double a = (parent1.Time == parent2.Time) ? 0.5 :
                            (this.Time - parent1.Time) / (parent2.Time - parent1.Time);
            this.Frame = Frame3f.Interpolate(parent1.Frame, parent2.Frame, (float)a);
        }

        public Keyframe(Keyframe copy)
        {
            Time = copy.Time;
            Frame = copy.Frame;
        }
    }




    public class KeyframeSequence
    {
        SortedList<double, Keyframe> Keys = new SortedList<double, Keyframe>();
        double validMin = 0;
        double validMax = double.MaxValue;
        

        public double MinTime {
            get { return validMin; }
        }
        public double MaxTime {
            get { return validMax; }
        }
        public Vector2d ValidRange {
            get { return new Vector2d(validMin, validMax); }
        }

        public void SetValidRange(double min, double max)
        {
            if (min >= max)
                throw new gException("KeyframeSequence.SetValidRange: invalid range min {0} max {1}", min, max);
            validMin = min;
            validMax = max;
        }


        public bool HasKeys {
            get { return Keys.Count > 0; }
        }
        public double FirstKeyTime {
            get { return (Keys.Count > 0) ? Keys.First().Key : 0; }
        }
        public double LastKeyTime {
            get { return (Keys.Count > 0) ? Keys.Last().Key : 0; }
        }
        public int Count {
            get { return Keys.Count; }
        }


        public OnChangeOpHandler ChangeOpEvent;


        void add_or_update_key(Keyframe f)
        {
            Keys[f.Time] = f;
        }
        void remove_key(double time)
        {
            Keys.Remove(time);
        }


        public bool AddKey(Keyframe f)
        {
            if ( f.Time < validMin || f.Time > validMax )
                throw new gException("KeyframeSequence.AddKey: time {0} is out of valid range", f.Time);
            if ( Keys.ContainsKey(f.Time) )
                throw new gException("KeyframeSequence.AddKey: key already exists at time {0}!", f.Time);

            add_or_update_key(f);
            UnityUtil.SafeSendEvent(ChangeOpEvent, this,
                new KeyframeAddRemoveChange() { key = f, sequence = this, bAdded = true });

            return true;
        }
        public bool AddOrUpdateKey(Keyframe f) {
            if ( f.Time < validMin || f.Time > validMax )
                throw new gException("KeyframeSequence.AddKey: time {0} is out of valid range", f.Time);

            IChangeOp change;
            if (Keys.ContainsKey(f.Time)) {
                Keyframe prev = Keys[f.Time];
                change = new KeyframeUpdateChange() { before = prev, after = f, sequence = this };
            } else {
                change = new KeyframeAddRemoveChange() { key = f, sequence = this, bAdded = true };
            }

            add_or_update_key(f);

            UnityUtil.SafeSendEvent(ChangeOpEvent, this, change);

            return true;
        }


        public bool RemoveKey(double time)
        {
            if ( Keys.ContainsKey(time) ) {
                Keyframe f = Keys[time];
                remove_key(time);
                UnityUtil.SafeSendEvent(ChangeOpEvent, this,
                    new KeyframeAddRemoveChange() { key = f, sequence = this, bAdded = false });
                return true;
            }
            return false;
        }


        public bool HasKey(double time)
        {
            return Keys.ContainsKey(time);
        }


        public bool UpdateKey(Keyframe f)
        {
            if (Keys.ContainsKey(f.Time)) {
                Keyframe prev = Keys[f.Time];
                IChangeOp change = new KeyframeUpdateChange() { before = prev, after = f, sequence = this };
                Keys[f.Time] = f;
                UnityUtil.SafeSendEvent(ChangeOpEvent, this, change);
                return true;
            }
            return false;
        }


        public Keyframe GetFrameAtTime(double time)
        {
            if (time <= Keys.First().Key)
                return Keys.First().Value;
            else if (time >= Keys.Last().Key)
                return Keys.Last().Value;
            Keyframe f0, f1;
            find_keys(time, out f0, out f1);
            return new Keyframe(f0, f1, time);
        }


        // enumerate through keyframes
        public IEnumerator<Keyframe> GetEnumerator() {
            foreach (var v in Keys)
                yield return v.Value;
        }


        void find_keys(double time, out Keyframe f0, out Keyframe f1)
        {
            int n0 = -1, n1 = -1;
            for (int i = 0; i < Keys.Count; ++i) {
                Keyframe f = Keys.ElementAt(i).Value;
                if (time < f.Time) {
                    n0 = i-1;
                    n1 = i;
                    break;
                }
            }
            if (n0 < 0)
                throw new gException("KeyframeSequence.find_keys: time is before first key!");

            f0 = Keys.ElementAt(n0).Value;
            f1 = Keys.ElementAt(n1).Value;
        }










        // change ops for KeyframeSequence
        public class KeyframeAddRemoveChange : BaseChangeOp
        {
            public Keyframe key;
            public bool bAdded;
            public KeyframeSequence sequence;       // do we need to hold ref to this??

            public override string Identifier() { return "KeyframeAddedChange"; }

            public override OpStatus Apply() {
                if (bAdded)
                    sequence.add_or_update_key(key);
                else
                    sequence.remove_key(key.Time);
                return OpStatus.Success;
            }

            public override OpStatus Revert() {
                if (bAdded)
                    sequence.remove_key(key.Time);
                else
                    sequence.add_or_update_key(key);
                return OpStatus.Success;
            }

            public override OpStatus Cull() {
                return OpStatus.Success;
            }
        }



        public class KeyframeUpdateChange : BaseChangeOp
        {
            public Keyframe before, after;
            public KeyframeSequence sequence;       // do we need to hold ref to this??

            public override string Identifier() { return "KeyframeUpdateChange"; }

            public override OpStatus Apply() {
                sequence.add_or_update_key(after);
                return OpStatus.Success;
            }

            public override OpStatus Revert() {
                sequence.add_or_update_key(before);
                return OpStatus.Success;
            }

            public override OpStatus Cull() {
                return OpStatus.Success;
            }
        }




    }




}
