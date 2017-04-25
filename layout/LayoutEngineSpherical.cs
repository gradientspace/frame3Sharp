using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    /// <summary>
    /// 3D layout engine... ?
    ///   - automatically uses fade in/out transitions on Add/Remove
    /// </summary>
    public class LayoutEngineSpherical : ILayoutEngine
    {
        public Cockpit Cockpit;
        public HUDContainer ScreenContainer;
        public HUDContainerLayout Layout;

        public float StandardDepth;

        struct LayoutItem
        {
            public SceneUIElement element;
            public LayoutFlags flags;
        }
        List<LayoutItem> items;


        public LayoutEngineSpherical(Cockpit parent, SphereRegion region)
        {
            this.Cockpit = parent;
            // TODO: who should own cylinder?
            ScreenContainer = new HUDContainer(new CockpitSphereContainerProvider(parent, region));
            Layout = new HUD3DSphereLayout(ScreenContainer, region);

            items = new List<LayoutItem>();
        }

        public float UIScaleFactor
        {
            get { return 1.0f; }
        }
        public IBoxModelElement BoxModelContainer
        {
            get { return this.ScreenContainer; }
        }


        bool has_item(SceneUIElement e)
        {
            return items.FindIndex((x) => { return x.element == e; }) >= 0;
        }
        void remove_item(SceneUIElement e)
        {
            int idx = items.FindIndex((x) => { return x.element == e; });
            if (idx >= 0)
                items.RemoveAt(idx);
        }
        bool find_item(SceneUIElement e, out LayoutItem item)
        {
            item = new LayoutItem();
            int idx = items.FindIndex((x) => { return x.element == e; });
            if (idx >= 0) {
                item = items[idx];
                return true;
            }
            return false;
        }


        public bool Contains(SceneUIElement element)
        {
            return has_item(element);
        }

        public void Add(SceneUIElement element, LayoutOptions options)
        {
            if ( has_item(element) ) 
                throw new Exception("LayoutEngineSpherical.Add: element is already in layout");
            if ( element is HUDStandardItem == false )
                throw new Exception("LayoutEngineSpherical.Add: element must be a HUDStandardItem");
            if ( element is IBoxModelElement == false ) 
                throw new Exception("LayoutEngineSpherical.Add: element must implement IBoxModelElement");

            LayoutItem newitem = new LayoutItem() { element = element, flags = options.Flags };
            items.Add(newitem);
            add(newitem.element as HUDStandardItem, options);

            // catch removals that did not go through Remove()
            element.OnDisconnected += Element_OnDisconnected;
        }


        // handle situations where someone else has removed the UIElement
        private void Element_OnDisconnected(object sender, EventArgs e)
        {
            SceneUIElement elem = sender as SceneUIElement;
            remove_item(elem);
        }


        public void RemoveAll(bool bDestroy)
        {
            foreach (var item in items)
                Remove(item.element, bDestroy);
        }


        public void Remove(SceneUIElement element, bool bDestroy)
        {
            LayoutItem item;
            bool bFound = find_item(element, out item);
            if ( bFound == false ) 
                throw new Exception("LayoutEngineSpherical.Remove: element is not in layout");
            if ( element is HUDStandardItem == false )
                throw new Exception("LayoutEngineSpherical.Add: element must be a HUDStandardItem");

            remove_item(element);
            Layout.RemoveLayoutItem(element);

            // this will remove from cockpit after transition
            if ((item.flags & LayoutFlags.AnimatedDismiss) != 0)
                HUDUtil.AnimatedDimiss_Cockpit(element as HUDStandardItem, this.Cockpit, bDestroy);
            else
                this.Cockpit.RemoveUIElement(element, bDestroy);
        }




        void add(HUDStandardItem element, LayoutOptions options)
        {
            if (element.IsVisible == false)
                element.IsVisible = true;

            IBoxModelElement elemBoxModel = element as IBoxModelElement;

            Frame3f viewFrame = Frame3f.Identity;

            element.SetObjectFrame(Frame3f.Identity);
            HUDUtil.PlaceInViewPlane(element, viewFrame);
            Cockpit.AddUIElement(element);

            Func<Vector2f> pinSourceF = options.PinSourcePoint2D;
            if (pinSourceF == null)
                pinSourceF = HUDLayoutUtil.BoxPointF(elemBoxModel, BoxPosition.Center);

            Func<Vector2f> pinTargetF = options.PinTargetPoint2D;
            if (pinTargetF == null)
                pinTargetF = HUDLayoutUtil.BoxPointF(ScreenContainer, BoxPosition.Center);

            Layout.AddLayoutItem(element, pinSourceF, pinTargetF, this.StandardDepth + options.DepthShift);

            // auto-show
            if ( (options.Flags & LayoutFlags.AnimatedShow) != 0 )
                HUDUtil.AnimatedShow(element);
        }


    }






    /// <summary>
    /// 2.5D box-model-ish layout for cylinder
    /// </summary>
    public class HUD3DSphereLayout : HUDContainerLayout
    {

        public SphereRegion Region;


        public HUD3DSphereLayout(HUDContainer container, SphereRegion sphere) : base(container)
        {
            Region = sphere;
        }

        protected override void layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            IBoxModelElement boxElem = e as IBoxModelElement;
            IElementFrame eFramed = e as IElementFrame;

            if (PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();

                Frame3f objF = eFramed.GetObjectFrame();
                Vector2f center2 = Region.To2DCoords(objF.Origin);

                Vector2f vOffset = SourcePos - center2;
                Vector2f vNewPos = PinToPos - vOffset;

                Frame3f frame = Region.From2DCoords(vNewPos, pin.fZ);
                eFramed.SetObjectFrame(frame);

            } else if (boxElem != null) {

                Frame3f frame = Region.From2DCoords(Vector2f.Zero, 0);
                eFramed.SetObjectFrame(frame);


            } else {
                // do nothing?
            }
        }
    }


}
