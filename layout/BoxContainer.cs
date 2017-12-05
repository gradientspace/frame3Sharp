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
    public interface IContainerBoundsProvider
    {
        AxisAlignedBox2f ContainerBounds { get; }
        event BoundsModifiedEventHandler OnContainerBoundsModified;
    }


    /// <summary>
    /// a BoxContainer is a domain that UI elements can exist inside of. 
    /// So the top-level window bounds would be a BoxContainer
    /// 
    /// [TODO] why do we have this class??? Couldn't we merge w/ IContainerBoundsProvider implementations?
    /// </summary>
    public class BoxContainer : IBoxModelElement, IDisposable
    {
        IContainerBoundsProvider Provider;
        AxisAlignedBox2f bounds;

        public event BoundsModifiedEventHandler OnContainerBoundsModified;

        public BoxContainer(IContainerBoundsProvider provider)
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





    /// <summary>
    /// just wraps an existing box element. But we need resize event...
    /// </summary>
    public class BoxModelElementContainerProvider : IContainerBoundsProvider, IDisposable
    {
        IBoxModelElement element;

        public BoxModelElementContainerProvider(IBoxModelElement element)
        {
            this.element = element;
        }
        public virtual void Dispose()
        {
        }

        public virtual AxisAlignedBox2f ContainerBounds
        {
            get { return element.Bounds2D; }
        }

        public virtual void PostOnModified()
        {
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }

        // how to fire??
        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }







    /// <summary>
    /// Provides 2D window dimensions as box container. Notifies when window is resized.
    /// </summary>
    public class Cockpit2DContainerProvider : IContainerBoundsProvider, IDisposable
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


        protected virtual void Context_OnWindowResized()
        {
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }


        public virtual AxisAlignedBox2f ContainerBounds
        {
            get { return cockpit.GetConstantSizeOrthoViewBounds();  }
        }


        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }




    /// <summary>
    /// Provides fixed BoxModelRegion as bounds, which could be a subregion of a 3D surface.
    /// Does *not* automatically notify of changes to Region, you can call NotifyParametersChanged() explicitly.
    /// </summary>
    public class BoxRegionContainerProvider : IContainerBoundsProvider, IDisposable
    {
        public Cockpit Cockpit;
        public ISurfaceBoxRegion Region;

        public BoxRegionContainerProvider(Cockpit c, ISurfaceBoxRegion region)
        {
            Cockpit = c;
            Region = region;
        }
        public virtual void Dispose()
        {
        }

        // [RMS] if you change parameters above after construction, you can call
        // this method and it will cause re-layout...
        public virtual void NotifyParametersChanged()
        {
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }

        public virtual AxisAlignedBox2f ContainerBounds
        {
            get {
                return Region.Bounds2D;
            }
        }


        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }



    


}
