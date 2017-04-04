using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    /// <summary>
    /// 2.5D Layout relative to a Container.
    /// Positions of Elements are fixed via Constraints. 
    /// Currently only supports Point-to-Point constraints.
    /// No understanding of dependencies !! 
    /// 
    /// HUDLayoutUtil can be used to easily construct point Func's for BoxModel elements
    /// 
    /// </summary>
    public class HUDContainerLayout : HUDLayout, IDisposable
    {
        HUDContainer container;

        struct Pin {
            public Func<Vector2f> FromF;
            public Func<Vector2f> ToF;
            public float fZ;
        };
        Dictionary<SceneUIElement, Pin> PinConstraints = new Dictionary<SceneUIElement, Pin>();


        /// <summary>
        /// by default we will try to layout each item when Added. If you
        /// disable this behavior, then you need to call RecomputeLayout() yourself.
        /// </summary>
        public bool LayoutItemOnAdd = true;


        public HUDContainerLayout(HUDContainer container)
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


        public HUDContainer Container
        {
            get { return container; }
        }


        /// <summary>
        /// Add item to layout and create Point-to-Point constraint, with optional Z
        /// When the layout is computed, we will find the translation from ElementPoint to PinTo, 
        /// and apply that translation to Element.
        /// 
        /// Example that will keep widget bottom-right at container bottom-right:
        ///    AddLayoutItem(hudWidget, 
        ///       HUDLayoutUtil.BoxPointF(hudWidget, BoxPosition.BottomRight),
        ///       HUDLayoutUtil.BoxPointF(Container, BoxPosition.BottomRight) );
        /// </summary>
        public void AddLayoutItem(SceneUIElement Element, Func<Vector2f> ElementPoint, Func<Vector2f> PinTo, float fZ = 0)
        {
            if (Element is IBoxModelElement == false)
                throw new Exception("HUDContainerLayout.AddLayoutItem : Element must implement IBoxModelElement!");

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
            AxisAlignedBox2f box = container.Bounds2D;

            foreach (SceneUIElement e in LayoutItems) {
                layout_item(e);
            }

        }



        void layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = container.Bounds2D;

            IBoxModelElement boxElem = e as IBoxModelElement;
            if ( PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();
                BoxModel.SetObjectPosition(boxElem, SourcePos, PinToPos, pin.fZ);

            } else if ( boxElem != null ) {
                BoxModel.SetObjectPosition(boxElem, BoxPosition.Center, box.Center, 0);

            } else {
                // do nothing?
            }
        }



    }
}
