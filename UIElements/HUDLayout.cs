using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using g3;

namespace f3
{

    /// <summary>
    /// Base interface for anything that can compute a Layout
    /// (maybe this is useless??)
    /// </summary>
    public interface ILayout
    {
        void RecomputeLayout();

        void AddLayoutItem(SceneUIElement element);
        bool RemoveLayoutItem(SceneUIElement element);
    }



    /// <summary>
    /// Standard base class for 2D layouts. Stores a set of SceneUIElement objects.
    /// </summary>
    public abstract class HUDLayout : ILayout
    {
        // force immediate layout recomputation
        public abstract void RecomputeLayout();


        protected List<SceneUIElement> LayoutItems = new List<SceneUIElement>();
        public ReadOnlyCollection<SceneUIElement> Items
        {
            get { return LayoutItems.AsReadOnly(); }
        }


        public virtual void AddLayoutItem(SceneUIElement element)
        {
            if (LayoutItems.Contains(element))
                throw new Exception("HUDLayout.AddLayoutItem: element " + element.Name + " already in layout");

            LayoutItems.Add(element);
        }

        public virtual bool RemoveLayoutItem(SceneUIElement element)
        {
            if ( LayoutItems.Contains(element) ) {
                LayoutItems.Remove(element);
                return true;
            }
            return false;
        }


    }





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
}
