using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// provides bounds to a HUDContainer
    /// </summary>
    public interface HUDContainerProvider
    {
        AxisAlignedBox2f ContainerBounds { get; }
        event BoundsModifiedEventHandler OnContainerBoundsModified;
    }


    /// <summary>
    /// a HUDContainer is a domain that UI elements can exist inside of. 
    /// So the top-level window bounds would be a HUDContainer
    /// </summary>
    public class HUDContainer : IBoxModelElement, IDisposable
    {
        HUDContainerProvider Provider;
        AxisAlignedBox2f bounds;

        public event BoundsModifiedEventHandler OnContainerBoundsModified;

        public HUDContainer(HUDContainerProvider provider)
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





    public class Cockpit2DContainerProvider : HUDContainerProvider, IDisposable
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
            DebugUtil.Log(2, "WINDOW RESIZE EVENT");
            FUtil.SafeSendAnyEvent(OnContainerBoundsModified, this);
        }


        public AxisAlignedBox2f ContainerBounds
        {
            get { return cockpit.GetOrthoViewBounds();  }
        }


        public event BoundsModifiedEventHandler OnContainerBoundsModified;
    }


}
