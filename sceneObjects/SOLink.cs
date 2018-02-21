using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public enum SOLinkType
    {
        OneWay, Symmetric
    }

    /// <summary>
    /// SOLink is a loose connection between to SOs. The idea here is that
    /// things can be quite general, and more complicated links can be built
    /// out of this. But not hugely tested right now...
    /// </summary>
    public interface SOLink
    {
        // link is not valid unless Manager is set
        SOLinkManager Manager { get; set; }

        // links are unique
        string UUID { get; }

        // name does not need to be initialized, but should never be
        // modified once link is registered w/ Manager
        string Name { get; }


        SceneObject Target { get; }
        SceneObject Source { get; }

        bool IsValid { get; }
        SOLinkType LinkType { get; }

        // remove connections
        void Unlink();

        /// <summary> You must implement this for SOLinkManager to automatically emit changes </summary>
        IChangeOp GetRemoveChange();
    }


    /// <summary>
    /// Implement base shared functionality for SOLink
    /// </summary>
    public abstract class StandardSOLink : SOLink
    {
        protected SOLinkManager manager = null;
        public SOLinkManager Manager {
            get { return manager; }
            set { manager = value; }
        }

        protected readonly string name = null;
        public string Name {
            get { return name; }
        }

        private readonly string uuid; 
        public string UUID {
            get { return uuid; }
        }


        public SceneObject Target { get; set; }
        public SceneObject Source { get; set; }

        public virtual bool IsValid { get { return Manager != null; } }
        public abstract SOLinkType LinkType { get; }

        public abstract IChangeOp GetRemoveChange();
        public abstract void Unlink();

        protected StandardSOLink()
        {
            uuid = System.Guid.NewGuid().ToString();
        }
    }


    

}
