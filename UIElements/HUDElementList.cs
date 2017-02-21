using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    class HUDElementList : HUDPanel
    {
        public float Spacing { get; set; }

        public enum ListDirection
        {
            Horizontal,
            Vertical
        }
        public ListDirection Direction { get; set; }


        List<SceneUIElement> ListItems = new List<SceneUIElement>();

        bool is_layout_valid;

        public HUDElementList()
        {
            Width = 100;
            Height = 100;
            Spacing = 10;
            Direction = ListDirection.Vertical;
            is_layout_valid = false;
        }


        public override void Disconnect()
        {
            
        }


        public virtual void AddListItem(SceneUIElement element)
        {
            if ( RootGameObject != null )
                throw new Exception("HUDElementList.AddElement: currently cannot add elements after calling Create");
            if (element is IBoxModelElement == false)
                throw new Exception("HUDElementList.AddElement: cannot add element " + element.RootGameObject.name + ", does not implement IBoxModelElement!");

            ListItems.Add(element);
            is_layout_valid = false;
        }



        public override void Create()
        {
            base.Create();

            //background = new HUDLabel() {
            //    Width = this.Width, Height = this.Height, BackgroundColor = Colorf.Silver, Text = ""
            //};
            //background.Create();
            //AddChild(background);

            foreach (SceneUIElement elem in ListItems)
                AddChild(elem, false);

            update_layout();
        }


        virtual public void InvalidateLayout()
        {
            is_layout_valid = false;
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



        void update_layout()
        {
            AxisAlignedBox2f contentBounds = BoxModel.PaddedContentBounds(this, Padding);
            Vector2f insertPos = contentBounds.TopLeft;

            int N = ListItems.Count;
            for ( int i = 0; i < N; ++i ) {
                IBoxModelElement boxelem = ListItems[i] as IBoxModelElement;
                if (ListItems[i].IsVisible == false) {
                    BoxModel.SetObjectPosition(boxelem, BoxPosition.TopLeft, contentBounds.TopLeft);
                    continue;
                }

                BoxModel.SetObjectPosition(boxelem, BoxPosition.TopLeft, insertPos);

                if ( Direction == ListDirection.Vertical )
                    insertPos.y -= boxelem.Size2D.y + Spacing;
                else
                    insertPos.x += boxelem.Size2D.y + Spacing;
            }

            is_layout_valid = true;
        }
    }
}
