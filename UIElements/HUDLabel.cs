using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using UnityEngine;

namespace f3
{
    // NB: on creation, this button is oriented so that positive Z points away from the *back* of
    //  this field (ie the field "faces" -Z). 
    public class HUDLabel : HUDStandardItem, IBoxModelElement
    {
        GameObject entry, bgMesh;
        fTextGameObject textMesh;

        public float Width { get; set; }
        public float Height { get; set; }
        public float TextHeight { get; set; }
        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }

        string text;
        public string Text
        {
            get { return text; }
            set { text = value; UpdateText(); }
        }

        public HUDLabel()
        {
            Width = 10;
            Height = 1;
            TextHeight = 0.8f;
            BackgroundColor = Color.white;
            TextColor = Color.black;
            text = "(entry)";
        }


        Mesh make_background_mesh()
        {
            return MeshGenerators.CreateTrivialRect(Width, Height, MeshGenerators.UVRegionType.FullUVSquare);
        }

        // creates a button in the desired geometry shape
        public void Create()
        {
            entry = new GameObject(UniqueNames.GetNext("HUDLabel"));
            bgMesh = AppendMeshGO("background", make_background_mesh(),
                MaterialUtil.CreateTransparentMaterial(BackgroundColor),
                entry);
            bgMesh.transform.Rotate(Vector3.right, -90.0f); // ??

            textMesh = GameObjectFactory.CreateTextMeshGO(
                "text", Text, TextColor, TextHeight,
                BoxPosition.CenterLeft );

            BoxModel.Translate(textMesh, Vector2f.Zero, this.Bounds2D.CenterLeft);

            AppendNewGO(textMesh, entry, false);
        }

        void UpdateText()
        {
            if (textMesh != null) {
                textMesh.SetColor(TextColor);
                textMesh.SetText(Text);
            }
        }

        // events for clicked and double-clicked.
        // NOTE: for doubleclick you will always receive an [OnClicked,OnDoubleClicked] pair.
        //    (alternative would be to delay OnClicked...perhaps that should be added as an option)
        public event EventHandler OnClicked;
        public event EventHandler OnDoubleClicked;


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
                    UnityUtil.SafeSendEvent(OnClicked, this, new EventArgs());
                    sent_click = true;
                    last_click_time = FPlatform.RealTime();
                } else {
                    float delta = FPlatform.RealTime() - last_click_time;
                    if (delta < double_click_delay) {
                        // if this second click comes fast enough, send doubleclick instead
                        UnityUtil.SafeSendEvent(OnDoubleClicked, this, new EventArgs());
                        sent_click = false;
                        last_click_time = 0.0f;
                    } else {
                        // send single-click and reset timer
                        UnityUtil.SafeSendEvent(OnClicked, this, new EventArgs());
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

}
