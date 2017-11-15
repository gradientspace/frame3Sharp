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
        protected abstract Vector3f layout_item(SceneUIElement e);


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


        protected Dictionary<SceneUIElement, List<Action<SceneUIElement>> > PostTransforms = 
            new Dictionary<SceneUIElement, List<Action<SceneUIElement>> >();

        // 2.5D layout positions (ie that we computed)
        protected Dictionary<SceneUIElement, Vector3f> LayoutPosition = new Dictionary<SceneUIElement, Vector3f>();


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

            if (LayoutItemOnAdd) {
                Vector3f result = layout_item(Element);
                LayoutPosition[Element] = result;
            }
        }


        public void AddPostTransform(SceneUIElement e, Action<SceneUIElement> xform)
        {
            if (PostTransforms.ContainsKey(e))
                PostTransforms[e].Add(xform);
            else
                PostTransforms[e] = new List<Action<SceneUIElement>>() { xform };
        }


        public override bool RemoveLayoutItem(SceneUIElement element)
        {
            bool bFound = base.RemoveLayoutItem(element);
            if (bFound)
                PinConstraints.Remove(element);

            if ( bFound ) {
                PostTransforms.Remove(element);
                LayoutPosition.Remove(element);
            }

            return bFound;
        }




        public override void RecomputeLayout()
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            foreach (SceneUIElement e in LayoutItems) {
                Vector3f result = layout_item(e);
                LayoutPosition[e] = result;
            }
        }


        public Vector2f GetLayoutCenter(SceneUIElement e)
        {
            Vector3f pos;
            if (LayoutPosition.TryGetValue(e, out pos))
                return pos.xy;
            return Vector2f.Zero;
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

        protected override Vector3f layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            Vector3f vNewPos3 = Vector3f.Zero;   // 2.5d center coordinate

            IBoxModelElement boxElem = e as IBoxModelElement;
            if (PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();
                vNewPos3 = BoxModel.SetObjectPosition(boxElem, SourcePos, PinToPos, pin.fZ);

            } else if (boxElem != null) {
                vNewPos3 = BoxModel.SetObjectPosition(boxElem, BoxPosition.Center, box.Center, 0);

            } else {
                // do nothing?
            }

            return vNewPos3;
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


        protected override Vector3f layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            IBoxModelElement boxElem = e as IBoxModelElement;
            IElementFrame eFramed = e as IElementFrame;

            Vector3f vNewPos3 = Vector3f.Zero;   // 2.5d center coordinate

            if (PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                // [TODO] We have to xform the center of the object. But if we pin
                //   a corner, we want to enforce the corner position in 3D (eg on curved surf).
                //   Currently we pin the corner in 2D, but then conver that to a 2D-center
                //   and then use that position. So on curved surf, things overlap, etc

                // evaluate pin constraints in 2D box space
                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();

                // map center of object into box space
                //  note: ignores orientation!
                //Frame3f objF = eFramed.GetObjectFrame();
                //Vector2f center2 = Region.To2DCoords(objF.Origin);
                Vector2f center2 = Vector2f.Zero;

                // construct new 2D position
                Vector2f vOffset = SourcePos - center2;
                Vector2f vNewPos = PinToPos - vOffset;
                vNewPos3 = new Vector3f(vNewPos.x, vNewPos.y, pin.fZ);

                // map 2D position back to 3D surface and orient object
                Frame3f frame = Region.From2DCoords(vNewPos, pin.fZ);
                eFramed.SetObjectFrame(frame);

            } else if (boxElem != null) {

                Vector2f vNewPos = Vector2f.Zero;
                vNewPos3 = new Vector3f(vNewPos.x, vNewPos.y, 0);

                // position object at center of box region
                Frame3f frame = Region.From2DCoords(vNewPos, 0);
                eFramed.SetObjectFrame(frame);


            } else {
                // do nothing?
            }

            if (PostTransforms.ContainsKey(e)) {
                foreach (var xform in PostTransforms[e])
                    xform(e);
            }

            return vNewPos3;
        }

    }






}
