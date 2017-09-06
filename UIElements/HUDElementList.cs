using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class HUDElementList : HUDPanel
    {
        public float Spacing { get; set; }

        public enum ListDirection
        {
            Horizontal,
            Vertical
        }
        public ListDirection Direction { get; set; }

        // HorzAligin only has effect when using Vertical direction, and vice-versa
        public HorizontalAlignment HorzAlign { get; set; }
        public VerticalAlignment VertAlign { get; set; }


        // these are computed when layout is updated
        public float VisibleListWidth = 0;
        public float VisibleListHeight = 0;


        public bool LimitItemsToBounds {
            get { return limit_items_to_bounds; }
            set { limit_items_to_bounds = value; InvalidateLayout(); }
        }
        bool limit_items_to_bounds = false;


        public int ScrollItems {
            get { return scroll_items; }
        }
        int scroll_items = 0;
        public int ScrollIndex {
            get { return scroll_index; }
            set { scroll_index = MathUtil.Clamp(value, 0, scroll_items); InvalidateLayout(); }
        }
        int scroll_index = 0;


        List<SceneUIElement> ListItems = new List<SceneUIElement>();
        List<Vector3f> ItemNudge = new List<Vector3f>();

        enum VisibleState { WasVisible, WasHidden, WasVisible_SetHidden }
        List<VisibleState> InternalVisibility = new List<VisibleState>();

        bool is_layout_valid;

        public HUDElementList()
        {
            Width = 100;
            Height = 100;
            Spacing = 10;
            Direction = ListDirection.Vertical;
            HorzAlign = HorizontalAlignment.Left;
            VertAlign = VerticalAlignment.Top;

            is_layout_valid = false;
        }


        public virtual void AddListItem(SceneUIElement element)
        {
            AddListItem(element, Vector3f.Zero);
        }
        public virtual void AddListItem(SceneUIElement element, Vector3f nudge)
        {
            if ( RootGameObject != null )
                throw new Exception("HUDElementList.AddElement: currently cannot add elements after calling Create");
            if (element is IBoxModelElement == false)
                throw new Exception("HUDElementList.AddElement: cannot add element " + element.RootGameObject.GetName() + ", does not implement IBoxModelElement!");

            ListItems.Add(element);
            ItemNudge.Add(nudge);
            InternalVisibility.Add( (element.IsVisible) ? VisibleState.WasVisible : VisibleState.WasHidden );
            InvalidateLayout();
        }



        public override void Create()
        {
            base.Create();

            foreach (SceneUIElement elem in ListItems)
                Children.Add(elem, false);

            update_layout();
        }


        virtual public void InvalidateLayout()
        {
            if (is_layout_valid) {
                is_layout_valid = false;
                for (int i = 0; i < ListItems.Count; ++i) {
                    if (InternalVisibility[i] == VisibleState.WasVisible_SetHidden)
                        ListItems[i].IsVisible = true;
                }
            }
        }
        virtual public void RecalculateLayout()
        {
            InvalidateLayout();
            update_layout();
        }


        override public void PreRender()
        {
            base.PreRender();
            if (is_layout_valid == false)
                update_layout();
        }



        protected virtual void update_layout()
        {
            int N = ListItems.Count;
            if (N == 0)
                return;

            // update initial visibility
            int total_visible = 0;
            for (int i = 0; i < ListItems.Count; ++i) {
                if (ListItems[i].IsVisible) {
                    InternalVisibility[i] = VisibleState.WasVisible;
                    total_visible++;
                } else
                    InternalVisibility[i] = VisibleState.WasHidden;
            }

            FixedBoxModelElement contentBounds = BoxModel.PaddedContentBounds(this, Padding);
            Vector2f topLeft = BoxModel.GetBoxPosition(contentBounds, BoxPosition.TopLeft);
            Vector2f insertPos = topLeft;

            int Nstop = -1;
            int iStart = 0;
            int actual_visible = 0;
            float spaceRequired = 0;
            float availableSpace = (Direction == ListDirection.Vertical) ? Height : Width;
            int li = 0;
            if (limit_items_to_bounds) {
                int hid = 0;
                while ( li < N && hid < scroll_index ) {
                    if ( InternalVisibility[li] == VisibleState.WasVisible ) {
                        InternalVisibility[li] = VisibleState.WasVisible_SetHidden;
                        ListItems[li].IsVisible = false;
                        hid++;
                    }
                    li++;
                }
            }
            while (li < N) {
                if (InternalVisibility[li] == VisibleState.WasHidden) {
                    li++;
                    continue;
                }
                if ( Nstop >= 0 ) {
                    InternalVisibility[li] = VisibleState.WasVisible_SetHidden;
                    ListItems[li].IsVisible = false;
                    li++;
                    continue;
                }

                actual_visible++;
                IBoxModelElement boxelem = ListItems[li] as IBoxModelElement;
                spaceRequired += (Direction == ListDirection.Vertical) ? boxelem.Size2D.y : boxelem.Size2D.x;
                if (limit_items_to_bounds && spaceRequired > availableSpace) {
                    InternalVisibility[li] = VisibleState.WasVisible_SetHidden;
                    ListItems[li].IsVisible = false;
                    Nstop = li;
                    spaceRequired = availableSpace;
                    actual_visible--;
                } else if (li < N - 1) {
                    spaceRequired += Spacing;
                }
                ++li;
            }
            if ( Direction == ListDirection.Vertical ) {
                VisibleListHeight = spaceRequired;
                VisibleListWidth = Width;
            } else {
                VisibleListHeight = Height;
                VisibleListWidth = spaceRequired;
            }

            scroll_items = 0;
            if (limit_items_to_bounds)
                scroll_items = total_visible - actual_visible;

            BoxPosition sourcePos = BoxPosition.TopLeft;
            if (Direction == ListDirection.Horizontal) {
                if (VertAlign == VerticalAlignment.Center) {
                    sourcePos = BoxPosition.CenterLeft;
                    insertPos = BoxModel.GetBoxPosition(contentBounds, BoxPosition.CenterLeft);
                } else if (VertAlign == VerticalAlignment.Bottom) {
                    sourcePos = BoxPosition.BottomLeft;
                    insertPos = BoxModel.GetBoxPosition(contentBounds, BoxPosition.BottomLeft);
                }
                if ( HorzAlign == HorizontalAlignment.Center ) {
                    insertPos.x += (this.Size2D.x - spaceRequired) / 2;
                } else if ( HorzAlign == HorizontalAlignment.Right ) {
                    insertPos.x += this.Size2D.x - spaceRequired;
                }

            } else {
                if (HorzAlign == HorizontalAlignment.Center) {
                    sourcePos = BoxPosition.CenterTop;
                    insertPos = BoxModel.GetBoxPosition(contentBounds, BoxPosition.CenterTop);
                } else if (HorzAlign == HorizontalAlignment.Right) {
                    sourcePos = BoxPosition.TopRight;
                    insertPos = BoxModel.GetBoxPosition(contentBounds, BoxPosition.TopRight);
                }
            }
                        

            for ( int i = 0; i < N; ++i ) {
                IBoxModelElement boxelem = ListItems[i] as IBoxModelElement;
                if (ListItems[i].IsVisible == false) {
                    BoxModel.SetObjectPosition(boxelem, BoxPosition.TopLeft, topLeft);
                    continue;
                }
                Vector2f usePos = insertPos + ItemNudge[i].xy;
                BoxModel.SetObjectPosition(boxelem, sourcePos, usePos, ItemNudge[i].z);

                if ( Direction == ListDirection.Vertical )
                    insertPos.y -= boxelem.Size2D.y + Spacing;
                else
                    insertPos.x += boxelem.Size2D.x + Spacing;
            }

            is_layout_valid = true;
        }
    }
}
