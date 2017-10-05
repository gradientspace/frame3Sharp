using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// This element is just a spacer. It has an empty game object which allows it to be positioned/etc.
    /// </summary>
    public class HUDSpacer : HUDStandardItem, IBoxModelElement
    {
        fGameObject entry;

        HUDShape shape;
        public HUDShape Shape {
            get { return shape; }
            set { shape = value; }
        }

        public HUDSpacer()
        {
            Shape = new HUDShape(HUDShapeType.Rectangle, 10, 1);
        }


        // creates a button in the desired geometry shape
        public void Create()
        {
            entry = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDSpacer"));
        }

        #region SceneUIElement implementation

        override public fGameObject RootGameObject {
            get { return entry; }
        }

        override public bool WantsCapture(InputEvent e)
        {
            return false;
        }

        override public bool BeginCapture(InputEvent e)
        {
            return true;
        }

        override public bool UpdateCapture(InputEvent e)
        {
            return true;
        }

        override public bool EndCapture(InputEvent e)
        {
            return true;
        }

        #endregion




        #region IBoxModelElement implementation


        public Vector2f Size2D {
            get { return new Vector2f(Shape.Width, Shape.Height); }
        }

        public AxisAlignedBox2f Bounds2D {
            get {
                Vector2f origin2 = RootGameObject.GetLocalPosition().xy;
                return new AxisAlignedBox2f(origin2, Shape.Width / 2, Shape.Height / 2);
            }
        }


        #endregion


    }

}
