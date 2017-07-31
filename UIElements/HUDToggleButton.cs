using System;
using System.Diagnostics;
using g3;

namespace f3
{
	// NB: on creation, this button is oriented so that positive Z points away from the *back* of
	//  this button (ie the button "faces" -Z). Important if you want to align button towards something!
	public class HUDToggleButton : HUDStandardItem, IBoxModelElement
	{
		fGameObject button, buttonMesh;
        fTextGameObject labelMesh;

        HUDToggleGroup parentGroup;

        public HUDShape Shape { get; set; }

        string text;
        public string Text
        {
            get { return text; }
            set { text = value; UpdateText(); }
        }
        public float TextHeight { get; set; }
        public Colorf TextColor { get; set; }


        public HUDToggleButton()
		{
            Shape = new HUDShape(HUDShapeType.Disc, 0.1f);
            bChecked = true;

            TextHeight = 0.8f;
            TextColor = Colorf.Black;
            text = "";
        }

        bool bChecked;
        public bool Checked {
            get { return bChecked; }
            set {
                if (bChecked != value) {
                    bChecked = value; SendOnToggled();
                }
            }
        }




        public void Create( fMaterial defaultMaterial ) {
			button = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDToggleButton"));
            buttonMesh = AppendMeshGO ("shape", HUDUtil.MakeBackgroundMesh(this.Shape), 
				defaultMaterial, button);

            buttonMesh.RotateD(Vector3f.AxisX, -90.0f); // ??

            if (text.Length > 0)
                UpdateText();
		}


        public void AddToGroup(HUDToggleGroup group)
        {
            Debug.Assert(parentGroup == null);
            parentGroup = group;
        }


		// event handler for clicked event
		public delegate void HUDToggleEventHandler(object sender, bool bEnabled);
		public event HUDToggleEventHandler OnToggled;

		protected virtual void SendOnToggled() {
			var tmp = OnToggled;
			if ( tmp != null ) tmp(this, Checked);
		}



        void UpdateText()
        {
            if (button == null)
                return;
            if ( labelMesh == null && text.Length > 0 ) {
                labelMesh =  GameObjectFactory.CreateTextMeshGO(
                    "label", Text, TextColor, TextHeight,
                    BoxPosition.Center, SceneGraphConfig.TextLabelZOffset );
                BoxModel.Translate(labelMesh, Vector2f.Zero, this.Bounds2D.Center);
                AppendNewGO(labelMesh, button, false);
            }

            if ( labelMesh != null) {
                labelMesh.SetColor(TextColor);
                labelMesh.SetText(Text);
            }
        }




		#region SceneUIElement implementation

		override public fGameObject RootGameObject {
			get { return button; }
		}

        override public bool WantsCapture(InputEvent e)
        {
            return (Enabled && HasGO(e.hit.hitGO));
        }

        override public bool BeginCapture(InputEvent e)
        {
            return true;
        }

        override public bool UpdateCapture (InputEvent e)
		{
			return true;
		}

		override public bool EndCapture (InputEvent e)
		{
            if (Parent == null) {
                DebugUtil.Log(2, "HUDToggleButton.EndCapture: our parent went invalid while we were capturing!");
                return true;
            }

            if (IsGOHit (e.ray, buttonMesh)) {
                // this is a bit hacky...if we are in a group, then we only toggle disabled -> enabled on click,
                // not enabled->disabled
                if ( parentGroup != null) {
                    if (Checked == false)
                        Checked = true;
                } else
                    Checked = !Checked;
			}
			return true;
		}

        #endregion


        #region IBoxModelElement implementation


        public Vector2f Size2D {
            get { return Shape.Size; }
        }

        public AxisAlignedBox2f Bounds2D {
            get {
                Vector2f origin2 = RootGameObject.GetLocalPosition().xy;
                Vector2f v = Shape.Size;
                return new AxisAlignedBox2f(origin2, v.x / 2, v.y / 2);
            }
        }


        #endregion

    }
}

