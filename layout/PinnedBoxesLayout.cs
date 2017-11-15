using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// Basic 2.5D cockpit UI layout engine, built on top of PinnedBoxesLayoutSolver
    /// Quirks:
    ///   - automatically uses fade in/out transitions on Add/Remove
    /// </summary>
    public class PinnedBoxesLayout : ICockpitLayout
    {
        public Cockpit Cockpit;
        public PinnedBoxesLayoutSolver Solver;

        public float StandardDepth;


        struct LayoutItem
        {
            public SceneUIElement element;
            public LayoutFlags flags;
        }
        List<LayoutItem> items;


        public PinnedBoxesLayout(Cockpit parent, PinnedBoxesLayoutSolver solver)
        {
            this.StandardDepth = 1.0f;

            this.Cockpit = parent;
            Solver = solver;

            items = new List<LayoutItem>();
        }


        virtual public Cockpit Parent
        {
            get { return Cockpit; }
        }

        // in VR layouts we were returning 1 here...??
        virtual public float UIScaleFactor
        {
            get { return Cockpit.GetPixelScale(); }
        }

        virtual public IBoxModelElement BoxElement
        {
            get { return Solver.Container; }
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
                throw new Exception("BoxModelLayoutEngine.Add: element is already in layout");
            if ( element is HUDStandardItem == false )
                throw new Exception("BoxModelLayoutEngine.Add: element must be a HUDStandardItem");
            if ( element is IBoxModelElement == false ) 
                throw new Exception("BoxModelLayoutEngine.Add: element must implement IBoxModelElement");

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
                throw new Exception("BoxModelLayoutEngine.Remove: element is not in layout");
            if ( element is HUDStandardItem == false )
                throw new Exception("BoxModelLayoutEngine.Add: element must be a HUDStandardItem");

            remove_item(element);
            Solver.RemoveLayoutItem(element);

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

            // for 2D view (but doesn't matter if we are doing a layout anyway!)
            Frame3f viewFrame = Cockpit.GetViewFrame2D();

            // with 3D view we should use this...
            //Frame3f viewFrame = Frame3f.Identity;

            element.SetObjectFrame(Frame3f.Identity);
            HUDUtil.PlaceInViewPlane(element, viewFrame);
            Cockpit.AddUIElement(element);

            Func<Vector2f> pinSourceF = options.PinSourcePoint2D;
            if (pinSourceF == null)
                pinSourceF = LayoutUtil.BoxPointF(elemBoxModel, BoxPosition.Center);

            Func<Vector2f> pinTargetF = options.PinTargetPoint2D;
            if (pinTargetF == null)
                pinTargetF = LayoutUtil.BoxPointF(Solver.Container, BoxPosition.Center);

            Solver.AddLayoutItem(element, pinSourceF, pinTargetF, this.StandardDepth + options.DepthShift);

            // if we want to shift result in its layout frame, do that via a post-transform
            if (options.FrameAxesShift != Vector3f.Zero) {
                Solver.AddPostTransform(element, (e) => {
                    Frame3f f = (e as IElementFrame).GetObjectFrame();
                    f.Translate(options.FrameAxesShift.x * f.X + options.FrameAxesShift.y * f.Y + options.FrameAxesShift.z * f.Z);
                    (e as IElementFrame).SetObjectFrame(f);
                });
            }
                

            // auto-show
            if ( (options.Flags & LayoutFlags.AnimatedShow) != 0 )
                HUDUtil.AnimatedShow(element);
        }






    }
}
