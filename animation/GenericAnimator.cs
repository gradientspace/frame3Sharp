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
        void Start();

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

            a.Start();
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



    /// <summary>
    /// GenericAnimatable is just an IAnimatable that can be registered
    /// with a GenericAnimator, that calls UpdateF each frame (which you provide).
    /// When you return true, the object is removed from the animation loop
    /// in the next frame.
    /// The point here is you can construct one of these inline, w/o having to subclass...
    /// </summary>
    public class GenericAnimatable : IAnimatable
    {
        bool deregister = false;

        /// <summary>
        /// You must replace this function, or this object is useless
        /// </summary>
        public Func<bool> UpdateF = () => { return true; };


        public GenericAnimatable() { }
        public GenericAnimatable(Func<bool> f)
        {
            UpdateF = f;
        }

        public virtual void Start()
        {
        }


        public virtual void Update()
        {
            if (UpdateF())
                deregister = true;
        }


        // if you return true, this animation is removed
        public virtual bool DeregisterNextFrame {
            get { return deregister; }
        }
    }


}
