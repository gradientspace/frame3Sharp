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
            foreach (IAnimatable a in remove)
                objects.Remove(a);
            remove.Clear();

            foreach (IAnimatable a in objects)
                a.Update();
        }

    }
}
