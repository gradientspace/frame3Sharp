using System;
using UnityEngine;
using g3;

namespace f3
{
	// NB: on creation, this button is oriented so that positive Z points away from the *back* of
	//  this button (ie the button "faces" -Z). Important if you want to align button towards something!
	public class HUDButton : HUDStandardItem
	{
		protected GameObject button, buttonMesh;

        public HUDShape Shape { get; set; }

        public HUDButton ()
		{
            Shape = new HUDShape() { Type = HUDShapeType.Disc, Radius = 0.1f };
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
                throw new Exception("[HUDButton::make_button_body_mesh] unknown shape type!");
            }
        }

        // creates a button in the desired geometry shape
		public void Create( Material defaultMaterial ) {
            button = new GameObject(UniqueNames.GetNext("HUDButton"));
			buttonMesh = AppendMeshGO ("disc", make_button_body_mesh(),
                defaultMaterial, button);

			buttonMesh.transform.Rotate (Vector3.right, -90.0f); // ??
		}

        // creates a button with a floating primitive in front of the button shape
		public void Create( PrimitiveType eType, Material bgMaterial, Material primMaterial, float fPrimScale = 0.7f  ) {
			button = new GameObject(UniqueNames.GetNext("HUDButton"));
            buttonMesh = AppendMeshGO ("disc", make_button_body_mesh(), bgMaterial, button);
			buttonMesh.transform.Rotate (Vector3.right, -90.0f); // ??

			GameObject prim = AppendUnityPrimitiveGO ("primitive", eType, primMaterial, button);
			float primSize = Shape.EffectiveRadius() * fPrimScale;
			prim.transform.localScale = new Vector3 (primSize, primSize, primSize);
			prim.transform.Translate (0.0f, 0.0f, - primSize);
			prim.transform.Rotate (-15.0f, 45.0f, 0.0f, Space.Self);
		}

        // creates a button that is just the mesh, basically same as above but without the background disc
        public void Create(PrimitiveType eType, Material primMaterial, float fPrimScale = 1.0f)
        {
            button = new GameObject(UniqueNames.GetNext("HUDButton"));

            buttonMesh = AppendUnityPrimitiveGO(UniqueNames.GetNext("HUDButton"), eType, primMaterial, button);
            float primSize = Shape.EffectiveRadius() * fPrimScale;
            buttonMesh.transform.localScale = new Vector3(primSize, primSize, primSize);
            buttonMesh.transform.Translate(0.0f, 0.0f, -primSize);
            buttonMesh.transform.Rotate(-15.0f, 45.0f, 0.0f, Space.Self);
        }

        // creates a button that is just the mesh
        public void Create( Mesh mesh, Material meshMaterial, float fScale, Quaternion transform )
        {
            button = new GameObject(UniqueNames.GetNext("HUDButton"));

            GameObject meshGO = AppendMeshGO("shape", mesh, meshMaterial, button);
            meshGO.transform.localScale = new Vector3(fScale, fScale, fScale);
            meshGO.transform.localRotation *= transform;
        }

        // creates a button with a background shape and a foreground mesh
        public void Create(Material bgMaterial, Mesh mesh, Material meshMaterial, float fScale, Frame3f deltaF)
        {
            button = new GameObject(UniqueNames.GetNext("HUDButton"));

            buttonMesh = AppendMeshGO("disc", make_button_body_mesh(), bgMaterial, button);
            buttonMesh.transform.Rotate(Vector3.right, -90.0f); // ??

            GameObject meshGO = AppendMeshGO("shape", mesh, meshMaterial, button);
            meshGO.transform.localScale = new Vector3(fScale, fScale, fScale);
            meshGO.transform.localPosition = deltaF.Origin;
            meshGO.transform.localRotation = deltaF.Rotation;
        }


        // events for clicked and double-clicked.
        // NOTE: for doubleclick you will always receive an [OnClicked,OnDoubleClicked] pair.
        //    (alternative would be to delay OnClicked...perhaps that should be added as an option)
        public event InputEventHandler OnClicked;
        public event InputEventHandler OnDoubleClicked;



		#region SceneUIElement implementation

		override public UnityEngine.GameObject RootGameObject {
			get { return button; }
		}



        override public bool WantsCapture(InputEvent e)
        {
            return (Enabled && HasGO(e.hit.hitGO));
        }

        override public bool BeginCapture (InputEvent e)
		{
            return true;
		}

		override public bool UpdateCapture (InputEvent e)
		{
			return true;
		}

        bool sent_click = false;
        float last_click_time = 0.0f;


		override public bool EndCapture (InputEvent e)
		{
			if (FindHitGO(e.ray)) {

                if (sent_click == false) {
                    // on first click we reset timer
                    UnityUtil.SafeSendEvent(OnClicked, this, e);
                    sent_click = true;
                    last_click_time = FPlatform.RealTime();
                } else {
                    float delta = FPlatform.RealTime() - last_click_time;
                    if ( delta < SceneGraphConfig.ActiveDoubleClickDelay ) {
                        // if this second click comes fast enough, send doubleclick instead
                        UnityUtil.SafeSendEvent(OnDoubleClicked, this, e);
                        sent_click = false;
                        last_click_time = 0.0f;
                    } else {
                        // send single-click and reset timer
                        UnityUtil.SafeSendEvent(OnClicked, this, e);
                        sent_click = true;
                        last_click_time = FPlatform.RealTime();
                    }
                }


            }
			return true;
		}

		#endregion
	}
}

