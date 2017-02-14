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

        List<SceneUIElement> ListItems = new List<SceneUIElement>();

        public HUDElementList()
        {
            Width = 100;
            Height = 100;
            Spacing = 10;
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
                AddChild(elem);

            update_layout();
        }


        void update_layout()
        {
            AxisAlignedBox2f frameBounds = this.Bounds2D;

            Vector2f insertPos = frameBounds.TopLeft;

            int N = ListItems.Count;
            for ( int i = 0; i < N; ++i ) {
                IBoxModelElement boxelem = ListItems[i] as IBoxModelElement;
                BoxModel.SetObjectPosition(boxelem, BoxPosition.TopLeft, insertPos);
                insertPos.y -= boxelem.Bounds2D.Height + Spacing;
            }
        }
    }
}
