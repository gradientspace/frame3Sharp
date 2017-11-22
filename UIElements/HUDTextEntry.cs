using System;
using g3;

namespace f3
{
    // NB: on creation, this button is oriented so that positive Z points away from the *back* of
    //  this field (ie the field "faces" -Z). Important if you want to align button towards something!
    public class HUDTextEntry : HUDStandardItem, IBoxModelElement
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
        public Colorf ActiveTextColor { get; set; }

        public HorizontalAlignment AlignmentHorz { get; set; }

        // by default HUDTextEntry will capture text input on click (via Context.RequestTextEntry)
        public bool OverrideDefaultInputHandling { get; set; }

        // set this to filter strings/etc. Arguments are old and new string,
        // you return filtered string.
        public Func<string, string, string> TextValidatorF = null;

        Colorf text_color;
        public Colorf TextColor {
            get { return text_color; }
            set { text_color = value; UpdateText(); }
        }

        string text;
        public string Text
        {
            get { return text; }
            set {
                string newText = validate_text(text, value);
                if (newText != text) {
                    text = newText;
                    UpdateText();
                    FUtil.SafeSendEvent(OnTextChanged, this, text);
                }
            }
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
            set { cursor_position = MathUtil.Clamp(value, 0, text.Length); }
        }

        fMaterial backgroundMaterial;
        fMaterial activeBackgroundMaterial;

        public HUDTextEntry()
        {
            Width = 10;
            Height = 1;
            TextHeight = 0.8f;
            BackgroundColor = Colorf.White;
            ActiveBackgroundColor = Colorf.Yellow; ;
            TextColor = Colorf.Black;
            ActiveTextColor = Colorf.Black;
            text = "(entry)";
            OverrideDefaultInputHandling = false;
        }



        // creates a button in the desired geometry shape
        public void Create()
        {
            entry = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDTextEntry"));
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
                "text", Text, TextColor, TextHeight, horzAlign, SceneGraphConfig.TextLabelZOffset );

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


        public bool SetText(string newText, bool bFromUserInput = true)
        {
            string validated = validate_text(text, newText);
            if (validated != text) {
                text = validated;
                UpdateText();
                FUtil.SafeSendEvent(OnTextChanged, this, text);
                if (bFromUserInput)
                    FUtil.SafeSendEvent(OnTextEdited, this, text);
                return true;
            }
            return false;
        }


        public void TakeFocus()
        {
            begin_editing();
        }



        string validate_text(string oldstring, string newString)
        {
            if (TextValidatorF != null)
                return TextValidatorF(oldstring, newString);
            return newString;
        }


        void UpdateText()
        {
            if (textMesh != null) {
                textMesh.SetText(Text);
                textMesh.SetColor(IsEditing ? ActiveTextColor : TextColor);
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
        public event TextChangedHander OnTextEdited;

        public event EditStateChangeHandler OnBeginTextEditing;
        public event EditStateChangeHandler OnEndTextEditing;



        void begin_editing()
        {
            // start capturing input
            if (OverrideDefaultInputHandling == false) {
                HUDTextEntryTarget entry = new HUDTextEntryTarget(this);
                if ( Parent.Context.RequestTextEntry(entry)) {

                    active_entry = entry;
                    FUtil.SafeSendEvent(OnBeginTextEditing, this);
                    bgMesh.SetMaterial(activeBackgroundMaterial);
                    textMesh.SetColor(ActiveTextColor);
                    start_active_time = FPlatform.RealTime();
                    cursor_position = text.Length;

                    entry.OnTextEditingEnded += (s, e) => {
                        bgMesh.SetMaterial(backgroundMaterial);
                        textMesh.SetColor(TextColor);
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




        // text editing functions
        public void BackspaceAtCursor()
        {
            if ( cursor_position > 0 ) {
                string new_text = text.Remove(cursor_position-1, 1);
                if ( SetText(new_text, true) )
                    cursor_position--;
            }
        }
        public void DeleteAtCursor()
        {
            if ( cursor_position < text.Length-1 ) {
                string new_text = text.Remove(cursor_position, 1);
                SetText(new_text, true);
            }
        }
        public void InsertAtCursor(string s)
        {
            string new_text = text.Insert(cursor_position, s);
            if (SetText(new_text, true))
                cursor_position += s.Length;
        }


    }





    class HUDTextEntryTarget : ITextEntryTarget
    {
        HUDTextEntry Entry;

        public EventHandler OnTextEditingEnded;

        public HUDTextEntryTarget(HUDTextEntry entry) {
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
            Entry.BackspaceAtCursor();
            return true;
        }

        public bool OnDelete()
        {
            Entry.DeleteAtCursor();
            return true;
        }

        public bool OnCharacters(string s)
        {
            Entry.InsertAtCursor(s);
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
            Entry.CursorPosition = Entry.CursorPosition - 1;
            return true;
        }
        public bool OnRightArrow()
        {
            Entry.CursorPosition = Entry.CursorPosition + 1;
            return true;
        }
    }



}
