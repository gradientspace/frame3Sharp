using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// provides bounds to a BoxContainer
    /// </summary>
    public interface ContainerBoundsProvider
    {
        AxisAlignedBox2f ContainerBounds { get; }
        event BoundsModifiedEventHandler OnContainerBoundsModified;
    }


    /// <summary>
    /// a BoxContainer is a domain that UI elements can exist inside of. 
    /// So the top-level window bounds would be a BoxContainer
    /// </summary>
    public class BoxContainer : IBoxModelElement, IDisposable
    {
        ContainerBoundsProvider Provider;
        AxisAlignedBox2f bounds;

        public event BoundsModifiedEventHandler OnContainerBoundsModified;

        public BoxContainer(ContainerBoundsProvider provider)
        {
            Provider = provider;
            Provider.OnContainerBoundsModified += OnProviderBoundsModified;
            bounds = Provider.ContainerBounds;
        }

        public virtual void Dispose()
        {
            Provider.OnContainerBoundsModified -= OnProviderBoundsModified;
        }


        private void OnProviderBoundsModified(object sender)
        {
            bounds = Provider.ContainerBounds;
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }


        public Vector2f Size2D {
            get {
                return Bounds2D.Diagonal;
            }
        }


        public AxisAlignedBox2f Bounds2D {
            get {
                return bounds;
            }
        }

    }





    public class Cockpit2DContainerProvider : ContainerBoundsProvider, IDisposable
    {
        Cockpit cockpit;

        public Cockpit2DContainerProvider(Cockpit c)
        {
            cockpit = c;
            cockpit.Context.OnWindowResized += Context_OnWindowResized;
        }
        public virtual void Dispose()
        {
            cockpit.Context.OnWindowResized -= Context_OnWindowResized;
        }


        private void Context_OnWindowResized()
        {
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }


        public AxisAlignedBox2f ContainerBounds
        {
            get { return cockpit.GetOrthoViewBounds();  }
        }


        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }







    public class CockpitCylinderContainerProvider : ContainerBoundsProvider, IDisposable
    {
        Cockpit cockpit;

        // should this go in container instead??
        public CylinderBoxRegion Cylinder;

        public CockpitCylinderContainerProvider(Cockpit c, CylinderBoxRegion cylinder)
        {
            cockpit = c;
            Cylinder = cylinder;
        }
        public virtual void Dispose()
        {
        }

        // [RMS] if you change parameters above after construction, you can call
        // this method and it will cause re-layout...
        public void NotifyParametersChanged()
        {
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }

        public AxisAlignedBox2f ContainerBounds
        {
            get {
                return Cylinder.Bounds2D;
            }
        }


        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }



    public class CockpitSphereContainerProvider : ContainerBoundsProvider, IDisposable
    {
        Cockpit cockpit;

        // should this go in container instead??
        public SphereBoxRegion Region;

        public CockpitSphereContainerProvider(Cockpit c, SphereBoxRegion region)
        {
            cockpit = c;
            Region = region;
        }
        public virtual void Dispose()
        {
        }

        // [RMS] if you change parameters above after construction, you can call
        // this method and it will cause re-layout...
        public void NotifyParametersChanged()
        {
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }

        public AxisAlignedBox2f ContainerBounds
        {
            get {
                return Region.Bounds2D;
            }
        }


        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }



}
