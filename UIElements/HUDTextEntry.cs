using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    // NB: on creation, this button is oriented so that positive Z points away from the *back* of
    //  this field (ie the field "faces" -Z). Important if you want to align button towards something!
    public class HUDTextEntry : HUDStandardItem, IBoxModelElement
    {
        GameObject entry, bgMesh;
        fTextGameObject textMesh;

        public float Width { get; set; }
        public float Height { get; set; }
        public float TextHeight { get; set; }
        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }

        // by default HUDTextEntry will capture text input on click (via Cockpit.RequestTextEntry)
        public bool OverrideDefaultInputHandling { get; set; }

        // set this to filter strings/etc. Arguments are old and new string,
        // you return filtered string.
        public Func<string, string, string> TextValidatorF = null;

        string text;
        public string Text
        {
            get { return text; }
            set {
                text = validate_text(text, value);
                UpdateText();
            }
        }


        public HUDTextEntry()
        {
            Width = 10;
            Height = 1;
            TextHeight = 0.8f;
            BackgroundColor = Color.white;
            TextColor = Color.black;
            text = "(entry)";
            OverrideDefaultInputHandling = false;
        }


        string validate_text(string oldstring, string newString)
        {
            if (TextValidatorF != null)
                return TextValidatorF(oldstring, newString);
            return newString;
        }


        Mesh make_background_mesh()
        {
            return MeshGenerators.CreateTrivialRect(Width, Height, MeshGenerators.UVRegionType.FullUVSquare);
        }

        // creates a button in the desired geometry shape
        public void Create()
        {
            entry = new GameObject(UniqueNames.GetNext("HUDTextEntry"));
            bgMesh = AppendMeshGO("background", make_background_mesh(),
                MaterialUtil.CreateFlatMaterial(BackgroundColor),
                entry);
            bgMesh.transform.Rotate(Vector3.right, -90.0f); // ??

            textMesh = 
                //GameObjectFactory.CreateTextMeshGO(
                GameObjectFactory.CreateTextMeshProGO(
                "text", Text, TextColor, TextHeight,
                BoxPosition.CenterLeft );

            BoxModel.Translate(textMesh, Vector2f.Zero, this.Bounds2D.CenterLeft);

            AppendNewGO(textMesh, entry, false);
        }

        void UpdateText()
        {
            if (textMesh != null) {
                textMesh.SetText(Text);
                textMesh.SetColor(TextColor);
            }
        }

        // events for clicked and double-clicked.
        // NOTE: for doubleclick you will always receive an [OnClicked,OnDoubleClicked] pair.
        //    (alternative would be to delay OnClicked...perhaps that should be added as an option)
        public event EventHandler OnClicked;
        public event EventHandler OnDoubleClicked;


        void on_clicked()
        {
            // start capturing input
            if ( OverrideDefaultInputHandling == false )
                FContext.ActiveContext_HACK.ActiveCockpit.RequestTextEntry( new HUDTextEntryTarget(this) );

            UnityUtil.SafeSendEvent(OnClicked, this, new EventArgs());    
        }
        void on_double_clicked()
        {
            UnityUtil.SafeSendEvent(OnDoubleClicked, this, new EventArgs());    
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





    class HUDTextEntryTarget : ITextEntryTarget
    {
        HUDTextEntry Entry;
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
            return true;
        }

        public bool OnBackspace() {
            if (Entry.Text.Length > 0)
                Entry.Text = Entry.Text.Substring(0, Entry.Text.Length - 1);
            return true;
        }

        public bool OnDelete()
        {   // weird!
            if (Entry.Text.Length > 0)
                Entry.Text = Entry.Text.Substring(1, Entry.Text.Length - 1);
            return true;
        }

        public bool OnCharacters(string s)
        {
            Entry.Text += s;
            return true;
        }

        public bool OnEscape()
        {
            // hack for now
            FContext.ActiveContext_HACK.ActiveCockpit.ReleaseTextEntry(this);
            return true;
        }

        public bool OnReturn()
        {
            // hack for now
            FContext.ActiveContext_HACK.ActiveCockpit.ReleaseTextEntry(this);
            return true;
        }
    }



}
