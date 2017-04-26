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
    /// actual layout behavior. 
    /// </summary>
    public abstract class PinnedBoxesLayoutSolver : BaseLayoutSolver, IDisposable
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


        public PinnedBoxesLayoutSolver(BoxContainer container)
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








    /// <summary>
    /// Implements standard 2.5D box layout based on Pin constraints.
    /// TODO: refactor this to use identity ISurfaceBoxRegion, then we can
    /// merge w/ BoxModel3DR
    /// </summary>
    public class PinnedBoxes2DLayoutSolver : PinnedBoxesLayoutSolver
    {
        public PinnedBoxes2DLayoutSolver(BoxContainer container) : base(container)
        {
        }

        protected override void layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            IBoxModelElement boxElem = e as IBoxModelElement;
            if (PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();
                BoxModel.SetObjectPosition(boxElem, SourcePos, PinToPos, pin.fZ);

            } else if (boxElem != null) {
                BoxModel.SetObjectPosition(boxElem, BoxPosition.Center, box.Center, 0);

            } else {
                // do nothing?
            }
        }
    }




    /// <summary>
    /// Implements 2.5D boxmodel-style layout on a 3D surface via IBoxModelRegion3D. 
    /// Basically this works the same way as normal 2D box layout, except that we
    /// map from/to the 3D surface first, via the 3D region.
    /// </summary>
    public class PinnedBoxes3DLayoutSolver : PinnedBoxesLayoutSolver
    {
        public ISurfaceBoxRegion Region;


        public PinnedBoxes3DLayoutSolver(BoxContainer container, ISurfaceBoxRegion region) : base(container)
        {
            Region = region;
        }


        protected override void layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            IBoxModelElement boxElem = e as IBoxModelElement;
            IElementFrame eFramed = e as IElementFrame;

            if (PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                // evaluate pin constraints in 2D box space
                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();

                // map center of object into box space
                //  note: ignores orientation!
                Frame3f objF = eFramed.GetObjectFrame();
                Vector2f center2 = Region.To2DCoords(objF.Origin);

                // construct new 2D position
                Vector2f vOffset = SourcePos - center2;
                Vector2f vNewPos = PinToPos - vOffset;

                // map 2D position back to 3D surface and orient object
                Frame3f frame = Region.From2DCoords(vNewPos, pin.fZ);
                eFramed.SetObjectFrame(frame);

            } else if (boxElem != null) {

                // position object at center of box region
                Frame3f frame = Region.From2DCoords(Vector2f.Zero, 0);
                eFramed.SetObjectFrame(frame);


            } else {
                // do nothing?
            }
        }

    }






}
