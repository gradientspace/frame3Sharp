using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    /// <summary>
    /// Basic popup message box. 
    /// Note that most fields must be configured before calling Create()
    /// 
    /// TODO: refactor this to be based on HUDPanel, like HUDPopupDialog
    /// 
    /// </summary>
    public class HUDPopupMessage : HUDStandardItem, IBoxModelElement
    {
        fGameObject entry;
        fGameObject bgMesh;
        fTextGameObject titleTextMesh;
        fTextAreaGameObject textMesh;
        fTextGameObject footerTextMesh;

        public float Width { get; set; }
        public float Height { get; set; }
        public float TitleTextHeight { get; set; }
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

        Colorf title_text_color;
        public Colorf TitleTextColor {
            get { return title_text_color; }
            set { title_text_color = value; UpdateText(); }
        }

        public HorizontalAlignment Alignment { get; set; }
        public HorizontalAlignment TitleAlignment { get; set; }

        public float Padding = 0;


        string titleText;
        public string TitleText
        {
            get { return titleText; }
            set { titleText = value; UpdateText(); }
        }

        string text;
        public string Text
        {
            get { return text; }
            set { text = value; UpdateText(); }
        }


        public bool EnableClickToDismiss = true;
        public string DismissText = "(click to dismiss)";
        public Colorf DismissTextColor = Colorf.DimGrey;


        public HUDPopupMessage()
        {
            Width = 10;
            Height = 5;
            TitleTextHeight = 1.0f;
            TextHeight = 0.8f;
            BackgroundColor = Colorf.VideoWhite;
            TextColor = Colorf.VideoBlack;
            TitleTextColor = Colorf.VideoBlack;
            Alignment = HorizontalAlignment.Left;
            titleText = "";
            text = "This is a popup message!";
        }


        fMesh make_background_mesh()
        {
            return new fMesh(
                MeshGenerators.CreateTrivialRect(Width, Height, MeshGenerators.UVRegionType.FullUVSquare));
        }

        
        // actually create GO elements, etc
        public void Create()
        {
            entry = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDPopupMessage"));

            bgMesh = AppendMeshGO("background", make_background_mesh(),
                MaterialUtil.CreateFlatMaterialF(BackgroundColor),
                entry);
            bgMesh.RotateD(Vector3f.AxisX, -90.0f);


            IBoxModelElement contentArea = BoxModel.PaddedBounds(this, Padding);

            BoxPosition titleBoxPos = BoxModel.ToPosition(TitleAlignment, VerticalAlignment.Top);
            titleTextMesh = (titleText == "") ? null : GameObjectFactory.CreateTextMeshGO(
                    "title", TitleText, TextColor, TitleTextHeight, titleBoxPos, SceneGraphConfig.TextLabelZOffset );
            float fTitleHeight = 0;
            if (titleTextMesh != null) {
                Vector2f titleToPos = BoxModel.GetBoxPosition(contentArea, titleBoxPos);
                BoxModel.Translate(titleTextMesh, Vector2f.Zero, titleToPos);
                AppendNewGO(titleTextMesh, entry, false);
                fTitleHeight = TitleTextHeight;
            }

            IBoxModelElement messageArea = BoxModel.PaddedBounds(contentArea, 0, 0, 0, Padding+fTitleHeight);
            Vector2f textDims = messageArea.Size2D;

            BoxPosition textBoxPos = BoxModel.ToPosition(Alignment, VerticalAlignment.Top);
            textMesh = GameObjectFactory.CreateTextAreaGO(
                    "message", Text, TextColor, TextHeight, textDims, Alignment, textBoxPos, SceneGraphConfig.TextLabelZOffset );
            Vector2f textToPos = BoxModel.GetBoxPosition(messageArea, textBoxPos);
            BoxModel.Translate(textMesh, Vector2f.Zero, textToPos);
            AppendNewGO(textMesh, entry, false);



            if ( EnableClickToDismiss ) {
                footerTextMesh = GameObjectFactory.CreateTextMeshGO(
                        "footer", DismissText, DismissTextColor, TextHeight*0.5f, 
                        BoxPosition.CenterBottom, SceneGraphConfig.TextLabelZOffset );
                BoxModel.Translate(footerTextMesh, Vector2f.Zero,  BoxModel.GetBoxPosition(contentArea, BoxPosition.CenterBottom) );
                AppendNewGO(footerTextMesh, entry, false);
            }

        }


        public void Dismiss()
        {
            FUtil.SafeSendEvent(OnDismissed, this, new EventArgs());
        }


        void UpdateText()
        {
            if ( bgMesh != null ) {
                bgMesh.SetColor(BackgroundColor);
            }
            if (textMesh != null) {
                textMesh.SetColor( TextColor );
                textMesh.SetText(Text);
            }
            if (titleTextMesh != null) {
                titleTextMesh.SetColor( TitleTextColor );
                titleTextMesh.SetText(TitleText);
            }
        }



        // events for clicked and dismissed.
        public event EventHandler OnClicked;
        public event EventHandler OnDismissed;



        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            UpdateText();
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

        override public bool EndCapture(InputEvent e)
        {
            if (FindHitGO(e.ray)) {
                if (EnableClickToDismiss) {
                    Dismiss();
                } else {
                    // send single-click and reset timer
                    FUtil.SafeSendEvent(OnClicked, this, new EventArgs());
                }
            }
            return true;
        }


        #endregion




       #region IBoxModelElement implementation


        public Vector2f Size2D {
            get { return new Vector2f(Width, Height); }
        }

        public AxisAlignedBox2f Bounds2D { 
            get {
                Vector2f origin2 = RootGameObject.GetLocalPosition().xy;
                return new AxisAlignedBox2f(origin2, Width/2, Height/2);
            }
        }


        #endregion


    }

}
