using System;
using UnityEngine;

namespace f3
{
	// NB: on creation, this button is oriented so that positive Z points away from the *back* of
	//  this button (ie the button "faces" -Z). Important if you want to align button towards something!
	public class HUDToggleButton : HUDStandardItem
	{
		GameObject button, buttonMesh;
        HUDToggleGroup parentGroup;

        public HUDShape Shape { get; set; }

        public HUDToggleButton()
		{
            Shape = new HUDShape() { Type = HUDShapeType.Disc, Radius = 0.1f };
            bChecked = true;
        }

        bool bChecked;
        public bool Checked {
            get { return bChecked; }
            set { if (bChecked != value) { bChecked = value; SendOnToggled(); }  }
        }


        Mesh make_button_body_mesh()
        {
            if (Shape.Type == HUDShapeType.Disc) {
                return MeshGenerators.CreateTrivialDisc(Shape.Radius, Shape.Slices);
            } else if (Shape.Type == HUDShapeType.Rectangle) {
                return MeshGenerators.CreateTrivialRect(Shape.Width, Shape.Height,
                    Shape.UseUVSubRegion == true ?
                        MeshGenerators.UVRegionType.CenteredUVRectangle : MeshGenerators.UVRegionType.FullUVSquare);
            } else {
                throw new Exception("[HUDToggleButton::make_button_body_mesh] unknown shape type!");
            }
        }


        public void Create( Material defaultMaterial ) {
			button = new GameObject(UniqueNames.GetNext("HUDToggleButton"));
            buttonMesh = AppendMeshGO ("disc", make_button_body_mesh(), 
				defaultMaterial, button);

			buttonMesh.transform.Rotate (Vector3.right, -90.0f); // ??
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





		#region SceneUIElement implementation

		override public UnityEngine.GameObject RootGameObject {
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
	}
}

