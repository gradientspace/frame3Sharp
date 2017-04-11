using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    // NB: on creation, this button is oriented so that positive Z points away from the *back* of
    //  this field (ie the field "faces" -Z). 
    public class HUDLabel : HUDStandardItem, IBoxModelElement
    {
        fGameObject entry;
        fGameObject bgMesh;
        fTextGameObject textMesh;

        public HUDShape Shape { get; set; }

        public float TextHeight { get; set; }

        Colorf bg_color;
        public Colorf BackgroundColor {
            get { return bg_color; }
            set { bg_color = value;  UpdateText(); }
        }

        Colorf text_color;
        public Colorf TextColor {
            get { return text_color; }
            set { text_color = value; UpdateText(); }
        }

        Colorf disabled_text_color;
        public Colorf DisabledTextColor {
            get { return disabled_text_color; }
            set { disabled_text_color = value; UpdateText(); }
        }

        public enum HorizontalAlignment { Left, Center, Right }
        public HorizontalAlignment AlignmentHorz { get; set; }

        string text;
        public string Text
        {
            get { return text; }
            set { text = value; UpdateText(); }
        }

        public HUDLabel()
        {
            Shape = new HUDShape(HUDShapeType.Rectangle, 10, 1);
            TextHeight = 0.8f;

            BackgroundColor = Colorf.VideoWhite;
            TextColor = Colorf.VideoBlack;
            DisabledTextColor = Colorf.DimGrey;
            AlignmentHorz = HorizontalAlignment.Left;
            text = "(entry)";
        }


        // creates a button in the desired geometry shape
        public void Create()
        {
            entry = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDLabel"));

            bgMesh = new fGameObject(AppendMeshGO("background", HUDUtil.MakeBackgroundMesh(Shape),
                MaterialUtil.CreateFlatMaterialF(BackgroundColor),
                entry));
            bgMesh.RotateD(Vector3f.AxisX, -90.0f);

            BoxPosition horzAlign = BoxPosition.CenterLeft;
            if (AlignmentHorz == HorizontalAlignment.Center)
                horzAlign = BoxPosition.Center;
            else if (AlignmentHorz == HorizontalAlignment.Right)
                horzAlign = BoxPosition.CenterRight;

            textMesh = 
                GameObjectFactory.CreateTextMeshGO(
                "text", Text, TextColor, TextHeight, horzAlign, SceneGraphConfig.TextLabelZOffset );

            Vector2f toPos = BoxModel.GetBoxPosition(this, horzAlign);
            BoxModel.Translate(textMesh, Vector2f.Zero, toPos);

            AppendNewGO(textMesh, entry, false);
        }

        void UpdateText()
        {
            if ( bgMesh != null ) {
                bgMesh.SetColor(BackgroundColor);
            }
            if (textMesh != null) {
                textMesh.SetColor( (Enabled) ? TextColor : DisabledTextColor );
                textMesh.SetText(Text);
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
            UpdateText();
        }       



        #region SceneUIElement implementation

        override public UnityEngine.GameObject RootGameObject
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
