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


        public enum SizeModes
        {
            FixedSize_AllowOverflow,
            FixedSize_LimitItemsToBounds,
            AutoSizeToFit
        }
        public SizeModes SizeMode {
            get { return size_mode; }
            set { size_mode = value; InvalidateLayout(); }
        }
        SizeModes size_mode = SizeModes.FixedSize_AllowOverflow;



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

            // this is messy. does multiple things:
            //  - computes visible dimensions / required space
            //  - in limit-to-bounds mode, figures out how many items are visible,
            //     and hides items that should not be visible
            //  - ??
            int Nstop = -1;
            int actual_visible = 0;
            float spaceRequired = 0;
            float otherDimMax = 0;
            float availableSpace = (Direction == ListDirection.Vertical) ? Height : Width;
            int li = 0;
            if (size_mode == SizeModes.FixedSize_LimitItemsToBounds) {
                // skip first scroll_index items
                int items_hidden = 0;
                while ( li < N && items_hidden < scroll_index ) {
                    if ( InternalVisibility[li] == VisibleState.WasVisible ) {
                        InternalVisibility[li] = VisibleState.WasVisible_SetHidden;
                        ListItems[li].IsVisible = false;
                        items_hidden++;
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
                Vector2f elemSize = boxelem.Size2D;
                if (Direction == ListDirection.Vertical) {
                    spaceRequired += elemSize.y;
                    otherDimMax = Math.Max(otherDimMax, elemSize.x);
                } else {
                    spaceRequired += elemSize.x;
                    otherDimMax = Math.Max(otherDimMax, elemSize.y);
                }
                if (size_mode == SizeModes.FixedSize_LimitItemsToBounds && spaceRequired > availableSpace) {
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
                VisibleListWidth = otherDimMax;
            } else {
                VisibleListHeight = otherDimMax;
                VisibleListWidth = spaceRequired;
            }

            // in auto-size mode, we can auto-size now that we know dimensions
            if ( SizeMode == SizeModes.AutoSizeToFit ) {
                float auto_width = VisibleListWidth + 2 * Padding;
                float auto_height = VisibleListHeight + 2 * Padding;
                if (Math.Abs(Width - auto_width) > 0.001f || Math.Abs(Height - auto_height) > 0.001f) {
                    Width = VisibleListWidth + 2 * Padding;
                    Height = VisibleListHeight + 2 * Padding;
                }
            }

            // track number of items that fit in bounds
            scroll_items = 0;
            if (size_mode == SizeModes.FixedSize_LimitItemsToBounds)
                scroll_items = total_visible - actual_visible;

            // now do actual layout

            FixedBoxModelElement contentBounds = BoxModel.PaddedContentBounds(this, Padding);
            Vector2f topLeft = BoxModel.GetBoxPosition(contentBounds, BoxPosition.TopLeft);
            Vector2f insertPos = topLeft;

            // compute insertion position based on alignment settings
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
                        
            // position visible elements
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
