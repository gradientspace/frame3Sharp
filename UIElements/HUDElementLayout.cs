using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    /// <summary>
    /// Basic 2.5D panel UI layout engine, built on top of PinnedBoxesLayoutSolver
    /// </summary>
    public class HUDElementLayout : IElementLayout
    {
        public SceneUIElement parentElement;
        public IChildren<SceneUIElement> childSet;

        public PinnedBoxesLayoutSolver Solver;

        public float StandardDepth = 0;


        struct LayoutItem
        {
            public SceneUIElement element;
            public LayoutFlags flags;
        }
        List<LayoutItem> items;


        public HUDElementLayout(SceneUIElement parent, IBoxModelElement boxElement, IChildren<SceneUIElement> children )
        {
            initialize(parent, new BoxModelElementContainerProvider(boxElement), children);
        }
        public HUDElementLayout(SceneUIElement parent, IContainerBoundsProvider elementBoundsProvider, IChildren<SceneUIElement> children)
        {
            initialize(parent, elementBoundsProvider, children);
        }
        void initialize(SceneUIElement parent, IContainerBoundsProvider boundsProvider, IChildren<SceneUIElement> children)
        {
            this.StandardDepth = 0.0f;
            this.parentElement = parent;
            this.childSet = children;
            Solver = new PinnedBoxes2DLayoutSolver(
                new BoxContainer(boundsProvider));
            items = new List<LayoutItem>();
        }


        virtual public SceneUIElement Parent
        {
            get { return this.parentElement; }
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

            // [RMS] cannot do this because we do not have control over panel
            if (bDestroy) {
                childSet.Remove(element);
                // [TODO] how to destroy??
            }
        }




        void add(HUDStandardItem element, LayoutOptions options)
        {
            if (element.IsVisible == false)
                element.IsVisible = true;

            IBoxModelElement elemBoxModel = element as IBoxModelElement;

            childSet.Add(element);

            Func<Vector2f> pinSourceF = options.PinSourcePoint2D;
            if (pinSourceF == null)
                pinSourceF = LayoutUtil.BoxPointF(elemBoxModel, BoxPosition.Center);

            Func<Vector2f> pinTargetF = options.PinTargetPoint2D;
            if (pinTargetF == null)
                pinTargetF = LayoutUtil.BoxPointF(Solver.Container, BoxPosition.Center);

            Solver.AddLayoutItem(element, pinSourceF, pinTargetF, this.StandardDepth + options.DepthShift);
        }



    }
}
