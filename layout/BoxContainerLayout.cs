using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    /// <summary>
    /// abstract 2.5D Layout relative to a Container.
    /// Positions of Elements are fixed via Constraints. 
    /// Currently only supports Point-to-Point Pin constraints.
    /// No understanding of dependencies !! 
    /// 
    /// LayoutUtil can be used to easily construct point Func's for BoxModel elements
    /// 
    /// Need to implement layout_item in subclasses to specify
    /// actual layout behavior
    /// 
    /// </summary>
    public abstract class BoxContainerLayout : BaseLayout, IDisposable
    {
        // must implement these
        protected abstract void layout_item(SceneUIElement e);


        BoxContainer container;
        public BoxContainer Container {
            get { return container; }
        }

        protected struct Pin {
            public Func<Vector2f> FromF;
            public Func<Vector2f> ToF;
            public float fZ;
        };
        protected Dictionary<SceneUIElement, Pin> PinConstraints = new Dictionary<SceneUIElement, Pin>();


        /// <summary>
        /// by default we will try to layout each item when Added. If you
        /// disable this behavior, then you need to call RecomputeLayout() yourself.
        /// </summary>
        public bool LayoutItemOnAdd = true;


        public BoxContainerLayout(BoxContainer container)
        {
            this.container = container;
            this.container.OnContainerBoundsModified += Container_OnContainerBoundsModified;
        }

        public virtual void Dispose()
        {
            container.OnContainerBoundsModified -= Container_OnContainerBoundsModified;
        }

        private void Container_OnContainerBoundsModified(object sender)
        {
            // should we be deferring this until next frame, something like that??
            RecomputeLayout();
        }


        /// <summary>
        /// Add item to layout and create Point-to-Point constraint, with optional Z
        /// When the layout is computed, we will find the translation from ElementPoint to PinTo, 
        /// and apply that translation to Element.
        /// 
        /// Example that will keep widget bottom-right at container bottom-right:
        ///    AddLayoutItem(hudWidget, 
        ///       LayoutUtil.BoxPointF(hudWidget, BoxPosition.BottomRight),
        ///       LayoutUtil.BoxPointF(Container, BoxPosition.BottomRight) );
        /// </summary>
        public void AddLayoutItem(SceneUIElement Element, Func<Vector2f> ElementPoint, Func<Vector2f> PinTo, float fZ = 0)
        {
            if (Element is IBoxModelElement == false)
                throw new Exception("BoxContainerLayout.AddLayoutItem : Element must implement IBoxModelElement!");

            base.AddLayoutItem(Element);
            PinConstraints.Add(Element, new Pin() { FromF = ElementPoint, ToF = PinTo, fZ = fZ });

            if ( LayoutItemOnAdd )
                layout_item(Element);
        }



        public override bool RemoveLayoutItem(SceneUIElement element)
        {
            bool bFound = base.RemoveLayoutItem(element);
            if (bFound)
                PinConstraints.Remove(element);
            return bFound;
        }




        public override void RecomputeLayout()
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            foreach (SceneUIElement e in LayoutItems) {
                layout_item(e);
            }

        }


    }







}
