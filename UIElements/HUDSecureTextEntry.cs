using System;
using System.Security;
using g3;

namespace f3
{
    /// <summary>
    /// For password entry, etc. Does not show entered characters, stores internally in a SecureString
    /// </summary>
    public class HUDSecureTextEntry : HUDStandardItem, IBoxModelElement
    {
        fGameObject entry;
        fMeshGameObject bgMesh;
        fTextGameObject textMesh;
        fRectangleGameObject cursor;

        public float Width { get; set; }
        public float Height { get; set; }
        public float TextHeight { get; set; }
        public Colorf BackgroundColor { get; set; }
        public Colorf ActiveBackgroundColor { get; set; }

        public HorizontalAlignment AlignmentHorz { get; set; }

        // by default HUDSecureTextEntry will capture text input on click (via Context.RequestTextEntry)
        public bool OverrideDefaultInputHandling { get; set; }

        public Func<char, bool> CharacterValidatorF = null;

        Colorf text_color;
        public Colorf TextColor {
            get { return text_color; }
            set { text_color = value; UpdateText(); }
        }

        //string text;
        //public string Text
        //{
        //    get { return text; }
        //    set {
        //        string newText = validate_text(text, value);
        //        if (newText != text) {
        //            text = newText;
        //            UpdateText();
        //            FUtil.SafeSendEvent(OnTextChanged, this, text);
        //        }
        //    }
        //}

        string dummy_text;
        SecureString real_text;
        public SecureString SecureText
        {
            get { return real_text; }
        }
            

        public int TextLength
        {
            get { return real_text.Length; }
        }

        ITextEntryTarget active_entry = null;
        float start_active_time = 0;
        int cursor_position = 0;

        public bool IsEditing
        {
            get { return (active_entry != null); }
        }

        public int CursorPosition
        {
            get { return cursor_position; }
            set { cursor_position = MathUtil.Clamp(value, 0, dummy_text.Length); }
        }

        fMaterial backgroundMaterial;
        fMaterial activeBackgroundMaterial;

        public HUDSecureTextEntry()
        {
            Width = 10;
            Height = 1;
            TextHeight = 0.8f;
            BackgroundColor = Colorf.White;
            ActiveBackgroundColor = Colorf.Yellow; ;
            TextColor = Colorf.Black;
            dummy_text = "";
            real_text = new SecureString();
            OverrideDefaultInputHandling = false;
        }



        // creates a button in the desired geometry shape
        public void Create()
        {
            entry = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDSecureTextEntry"));
            fMesh mesh = MeshGenerators.CreateTrivialRect(Width, Height, MeshGenerators.UVRegionType.FullUVSquare);
            backgroundMaterial = MaterialUtil.CreateFlatMaterialF(BackgroundColor);
            activeBackgroundMaterial = MaterialUtil.CreateFlatMaterialF(ActiveBackgroundColor);
            bgMesh = AppendMeshGO("background", mesh, backgroundMaterial, entry);
            bgMesh.RotateD(Vector3f.AxisX, -90.0f); // ??

            BoxPosition horzAlign = BoxPosition.CenterLeft;
            if (AlignmentHorz == HorizontalAlignment.Center)
                horzAlign = BoxPosition.Center;
            else if (AlignmentHorz == HorizontalAlignment.Right)
                horzAlign = BoxPosition.CenterRight;

            textMesh = GameObjectFactory.CreateTextMeshGO(
                "text", "", TextColor, TextHeight, horzAlign, SceneGraphConfig.TextLabelZOffset );

            Vector2f toPos = BoxModel.GetBoxPosition(this, horzAlign);
            BoxModel.Translate(textMesh, Vector2f.Zero, toPos);

            AppendNewGO(textMesh, entry, false);

            cursor = GameObjectFactory.CreateRectangleGO("cursor", Height * 0.1f, Height * 0.8f, Colorf.VideoBlack, false);
            BoxModel.Translate(cursor, Vector2f.Zero, this.Bounds2D.CenterLeft, -Height*0.1f);
            cursor.RotateD(Vector3f.AxisX, -90.0f);
            AppendNewGO(cursor, entry, false);
            cursor.SetVisible(false);
        }


        public override void Disconnect()
        {
            if (active_entry != null) {
                Parent.Context.ReleaseTextEntry(active_entry);   // probably should not have to call this...
                active_entry = null;
            }
            base.Disconnect();
        }

        public bool AppendString(string s)
        {
            if (CharacterValidatorF != null) {
                for (int i = 0; i < s.Length; ++i) {
                    if (CharacterValidatorF(s[i]) == false)
                        return false;
                }
            }

            for (int i = 0; i < s.Length; ++i)
                AppendCharacter(s[i]);
            return true;
        }

        public bool AppendCharacter(char c)
        {
            if (CharacterValidatorF != null && CharacterValidatorF(c) == false)
                return false;

            real_text.AppendChar(c);
            dummy_text += "*";

            UpdateText();
            cursor_position = MathUtil.Clamp(cursor_position+1, 0, dummy_text.Length);

            FUtil.SafeSendEvent(OnTextChanged, this, dummy_text);
            return true;
        }

        public void PopCharacter()
        {
            if (real_text.Length > 0) {
                real_text.RemoveAt(real_text.Length - 1);
                dummy_text = dummy_text.Substring(0, real_text.Length);

                UpdateText();
                cursor_position = MathUtil.Clamp(cursor_position-1, 0, dummy_text.Length);

                FUtil.SafeSendEvent(OnTextChanged, this, dummy_text);
            }
        }


        public void Clear()
        {
            real_text = new SecureString();
            dummy_text = "";
            UpdateText();
            cursor_position = 0;
            FUtil.SafeSendEvent(OnTextChanged, this, dummy_text);
        }



        public void TakeFocus()
        {
            begin_editing();
        }



        //string validate_text(string oldstring, string newString)
        //{
        //    if (TextValidatorF != null)
        //        return TextValidatorF(oldstring, newString);
        //    return newString;
        //}


        void UpdateText()
        {
            if (textMesh != null) {
                textMesh.SetText(dummy_text);
                textMesh.SetColor(TextColor);
            }
        }


        public override void PreRender()
        {
            if (IsEditing) {
                // cursor blinks every 0.5s
                float dt = FPlatform.RealTime() - start_active_time;
                cursor.SetVisible( (int)(2 * dt) % 2 == 0 );

                // DO NOT DO THIS EVERY FRAME!!!
                BoxModel.MoveTo(cursor, this.Bounds2D.CenterLeft, -Height * 0.1f);
                Vector2f cursorPos = textMesh.TextObject.GetCursorPosition(cursor_position);
                BoxModel.Translate(cursor, cursorPos);
            } else {
                cursor.SetVisible(false);
            }
        }


        // events for clicked and double-clicked.
        // NOTE: for doubleclick you will always receive an [OnClicked,OnDoubleClicked] pair.
        //    (alternative would be to delay OnClicked...perhaps that should be added as an option)
        public event EventHandler OnClicked;
        public event EventHandler OnDoubleClicked;

        // this is sent whenever text changes - including programmatically
        public event TextChangedHander OnTextChanged;

        // this is sent when we get a text change from user input
        //public event TextChangedHander OnTextEdited;

        public event EditStateChangeHandler OnBeginTextEditing;
        public event EditStateChangeHandler OnEndTextEditing;



        void begin_editing()
        {
            // start capturing input
            if (OverrideDefaultInputHandling == false) {
                HUDSecureTextEntryTarget entry = new HUDSecureTextEntryTarget(this);
                if ( Parent.Context.RequestTextEntry(entry)) {

                    active_entry = entry;
                    FUtil.SafeSendEvent(OnBeginTextEditing, this);
                    bgMesh.SetMaterial(activeBackgroundMaterial);
                    start_active_time = FPlatform.RealTime();
                    cursor_position = dummy_text.Length;

                    entry.OnTextEditingEnded += (s, e) => {
                        bgMesh.SetMaterial(backgroundMaterial);
                        active_entry = null;
                        FUtil.SafeSendEvent(OnEndTextEditing, this);
                    };
                }
            }
        }



        void on_clicked()
        {
            begin_editing();

            FUtil.SafeSendEvent(OnClicked, this, new EventArgs());    
        }
        void on_double_clicked()
        {
            FUtil.SafeSendEvent(OnDoubleClicked, this, new EventArgs());    
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
            if (FindHitGO(e.ray)) {

                if (sent_click == false) {
                    // on first click we reset timer
                    on_clicked();
                    sent_click = true;
                    last_click_time = FPlatform.RealTime();
                } else {
                    float delta = FPlatform.RealTime() - last_click_time;
                    if (delta < double_click_delay) {
                        // if this second click comes fast enough, send doubleclick instead
                        on_double_clicked();
                        sent_click = false;
                        last_click_time = 0.0f;
                    } else {
                        // send single-click and reset timer
                        on_clicked();
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
            get { return new Vector2f(Width, Height); }
        }

        public AxisAlignedBox2f Bounds2D { 
            get { return new AxisAlignedBox2f(Vector2f.Zero, Width/2, Height/2); }
        }


        #endregion


    }





    class HUDSecureTextEntryTarget : ITextEntryTarget
    {
        HUDSecureTextEntry Entry;

        public EventHandler OnTextEditingEnded;

        public HUDSecureTextEntryTarget(HUDSecureTextEntry entry) {
            Entry = entry;
        }

        public bool ConsumeAllInput() {
            return true;
        }

        public bool OnBeginTextEntry() {
            return true;
        }
        public bool OnEndTextEntry() {
            if (OnTextEditingEnded != null)
                OnTextEditingEnded(this, null);
            return true;
        }

        public bool OnBackspace() {
            Entry.PopCharacter();
            return true;
        }

        public bool OnDelete()
        {
            Entry.Clear();
            return true;
        }

        public bool OnCharacters(string s)
        {
            Entry.AppendString(s);
            return true;
        }

        public bool OnEscape()
        {
            // hack for now
            Entry.Parent.Context.ReleaseTextEntry(this);
            OnEndTextEntry();
            return true;
        }

        public bool OnReturn()
        {
            // hack for now
            Entry.Parent.Context.ReleaseTextEntry(this);
            return true;
        }

        public bool OnLeftArrow()
        {
            // ignore
            return true;
        }
        public bool OnRightArrow()
        {
            // ignore
            return true;
        }
    }



}
