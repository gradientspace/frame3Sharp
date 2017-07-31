using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    // NB: on creation, this button is oriented so that positive Z points away from the *back* of
    //  this field (ie the field "faces" -Z). 

    /// <summary>
    /// This is just a HUDShape, optionally with a border. Use for backgrounds, etc, instead of HUDLabel
    /// </summary>
    public class HUDShapeElement : HUDStandardItem, IBoxModelElement
    {
        fGameObject entry;
        fGameObject bgMesh;
        fGameObject border;

        HUDShape shape;
        public HUDShape Shape
        {
            get { return shape; }
            set { update_shape(value); }
        }

        Colorf color;
        public Colorf Color {
            get { return color; }
            set { color = value; update_color(); }
        }

        // [TODO] expose these properly
        public bool EnableBorder = false;
        public float BorderWidth = 1.0f;
        public Colorf BorderColor = Colorf.DarkSlateGrey;

        public HUDShapeElement()
        {
            Shape = new HUDShape(HUDShapeType.Rectangle, 10, 1);
            Color = Colorf.VideoWhite;
        }


        // creates a button in the desired geometry shape
        public void Create()
        {
            entry = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDShapeElement"));

            bgMesh = new fGameObject(AppendMeshGO("background", HUDUtil.MakeBackgroundMesh(Shape),
                MaterialUtil.CreateFlatMaterialF(Color),
                entry));
            bgMesh.RotateD(Vector3f.AxisX, -90.0f);

            if ( EnableBorder ) {
                HUDShape borderShape = Shape;
                borderShape.Radius += BorderWidth;
                borderShape.Height += 2 * BorderWidth;
                borderShape.Width += 2 * BorderWidth;
                border = new fGameObject(AppendMeshGO("border", HUDUtil.MakeBackgroundMesh(borderShape),
                    MaterialUtil.CreateFlatMaterialF(BorderColor), entry));
                border.RotateD(Vector3f.AxisX, -90.0f);
                border.Translate(-0.001f * Vector3f.AxisY, true);
            }
        }

        void update_color()
        {
            if ( bgMesh != null ) {
                bgMesh.SetColor(Color);
            }
        }


        void update_shape(HUDShape newShape)
        {
            shape = newShape;
            if (bgMesh == null)
                return;
            bgMesh.SetSharedMesh(
                HUDUtil.MakeBackgroundMesh(shape));
            if ( EnableBorder ) {
                HUDShape borderShape = shape;
                borderShape.Radius += BorderWidth;
                borderShape.Height += 2 * BorderWidth;
                borderShape.Width += 2 * BorderWidth;
                border.SetSharedMesh(HUDUtil.MakeBackgroundMesh(borderShape));
            } 
        }


        // events for clicked and double-clicked.
        // NOTE: for doubleclick you will always receive an [OnClicked,OnDoubleClicked] pair.
        //    (alternative would be to delay OnClicked...perhaps that should be added as an option)
        public event EventHandler OnClicked;
        public event EventHandler OnDoubleClicked;

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            update_color();
        }       

        #region SceneUIElement implementation

        override public fGameObject RootGameObject
        {
            get { return entry; }
        }

        override public bool WantsCapture(InputEvent e)
        {
            return (Enabled && HasGO(e.hit.hitGO));
        }

        override public bool BeginCapture(InputEvent e)
        {
            return true;
        }

        override public bool UpdateCapture(InputEvent e)
        {
            return true;
        }

        bool sent_click = false;
        float last_click_time = 0.0f;
        float double_click_delay = 0.3f;


        override public bool EndCapture(InputEvent e)
        {
            if (Parent == null) {
                DebugUtil.Log(2, "HUDShapeElement.EndCapture: our parent went invalid while we were capturing!");
                return true;
            }

            if (FindHitGO(e.ray)) {

                if (sent_click == false) {
                    // on first click we reset timer
                    FUtil.SafeSendEvent(OnClicked, this, new EventArgs());
                    sent_click = true;
                    last_click_time = FPlatform.RealTime();
                } else {
                    float delta = FPlatform.RealTime() - last_click_time;
                    if (delta < double_click_delay) {
                        // if this second click comes fast enough, send doubleclick instead
                        FUtil.SafeSendEvent(OnDoubleClicked, this, new EventArgs());
                        sent_click = false;
                        last_click_time = 0.0f;
                    } else {
                        // send single-click and reset timer
                        FUtil.SafeSendEvent(OnClicked, this, new EventArgs());
                        sent_click = true;
                        last_click_time = FPlatform.RealTime();
                    }
                }


            }
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
                return new AxisAlignedBox2f(origin2, Shape.Width/2, Shape.Height/2);
            }
        }


        #endregion


    }

}
