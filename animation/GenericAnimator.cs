using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{

    public interface IPerFrameAnimator
    {
        void NextFrame();
    }



    public interface IAnimatable
    {
        void Update();

        // if you return true, this animation is removed
        bool DeregisterNextFrame { get; }
    }


    public class GenericAnimator : IPerFrameAnimator
    {
        List<IAnimatable> objects = new List<IAnimatable>();

        List<IAnimatable> remove = new List<IAnimatable>();


        public void Register(IAnimatable a)
        {
            if (objects.Contains(a))
                throw new Exception("GenericAnimator.Register: already contains object!!");
            objects.Add(a);
        }



        public void DeregisterNextFrame(IAnimatable a)
        {
            remove.Add(a);
        }


        public void NextFrame()
        {
            int nObjects = objects.Count;
            for ( int i = 0; i < nObjects; ++i ) {
                IAnimatable a = objects[i];
                if (a.DeregisterNextFrame)
                    remove.Add(a);
            }

            int nRemove = remove.Count;
            for ( int i = 0; i < nRemove; ++i ) {
                IAnimatable a = remove[i];
                objects.Remove(a);
            }
            remove.Clear();

            nObjects = objects.Count;
            for (int i = 0; i < nObjects; ++i) {
                IAnimatable a = objects[i];
                a.Update();
            }
        }

    }
}
