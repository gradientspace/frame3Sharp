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
    }




    public class KeyframeSequence
    {
        SortedList<double, Keyframe> Keys = new SortedList<double, Keyframe>();
        double validMin = 0;
        double validMax = double.MaxValue;
        

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


        public bool AddKey(Keyframe f, bool bReplace = false)
        {
            if (f.Time < validMin || f.Time > validMax)
                throw new gException("KeyframeSequence.AddKey: time {0} is out of valid range", f.Time);
            if (Keys.ContainsKey(f.Time) && bReplace == false)
                throw new gException("KeyframeSequence.AddKey: key already exists at time {0}!", f.Time);

            Keys[f.Time] = f;
            return true;
        }
        public bool AddOrUpdateKey(Keyframe f) {
            return AddKey(f, true);
        }


        public bool RemoveKey(double time)
        {
            if (Keys.ContainsKey(time) == false)
                return false;
            Keys.Remove(time);
            return true;
        }


        public bool HasKey(double time)
        {
            return Keys.ContainsKey(time);
        }


        public bool UpdateKey(Keyframe f)
        {
            if (Keys.ContainsKey(f.Time)) {
                Keys[f.Time] = f;
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


    }
}
