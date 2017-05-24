using System;
using UnityEngine;
using g3;

namespace f3
{
	// NB: on creation, this button is oriented so that positive Z points away from the *back* of
	//  this button (ie the button "faces" -Z). Important if you want to align button towards something!
	public class HUDButton : HUDStandardItem, IBoxModelElement
	{
		protected GameObject button, buttonMesh;
        protected fMaterial standard_mat, disabled_mat;

        public fMaterial StandardMaterial
        {
            get { return standard_mat; }
            set { standard_mat = value;  update_material(); }
        }

        public fMaterial DisabledMaterial
        {
            get { return disabled_mat; }
            set { disabled_mat = value;  update_material(); }
        }


        public HUDShape Shape { get; set; }


        public HUDButton ()
		{
            Shape = new HUDShape(HUDShapeType.Disc, 0.1f );
		}


        // creates a button in the desired geometry shape
		public void Create( Material defaultMaterial, Material disabledMaterial = null ) {
            button = new GameObject(UniqueNames.GetNext("HUDButton"));
			buttonMesh = AppendMeshGO ("shape", HUDUtil.MakeBackgroundMesh(this.Shape),
                defaultMaterial, button);

			buttonMesh.transform.Rotate (Vector3.right, -90.0f); // ??
            MaterialUtil.DisableShadows(buttonMesh);

            standard_mat = new fMaterial(defaultMaterial);
            if (disabledMaterial != null)
                disabled_mat = new fMaterial(disabledMaterial);
        }

        // creates a button with a floating primitive in front of the button shape
        public void Create( PrimitiveType eType, Material bgMaterial, Material primMaterial, float fPrimScale = 0.7f  ) {
			button = new GameObject(UniqueNames.GetNext("HUDButton"));
            buttonMesh = AppendMeshGO ("shape", HUDUtil.MakeBackgroundMesh(this.Shape), bgMaterial, button);
			buttonMesh.transform.Rotate (Vector3.right, -90.0f); // ??
            MaterialUtil.DisableShadows(buttonMesh);

            GameObject prim = AppendUnityPrimitiveGO ("primitive", eType, primMaterial, button);
			float primSize = Shape.EffectiveRadius() * fPrimScale;
			prim.transform.localScale = new Vector3 (primSize, primSize, primSize);
			prim.transform.Translate (0.0f, 0.0f, - primSize);
			prim.transform.Rotate (-15.0f, 45.0f, 0.0f, Space.Self);
            MaterialUtil.DisableShadows(prim);

            standard_mat = new fMaterial(bgMaterial);
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
            MaterialUtil.DisableShadows(buttonMesh);

            standard_mat = new fMaterial(primMaterial);
        }

        // creates a button that is just the mesh
        public void Create( fMesh mesh, Material meshMaterial, float fScale, Quaternion transform )
        {
            button = new GameObject(UniqueNames.GetNext("HUDButton"));

            GameObject meshGO = AppendMeshGO("shape", mesh, meshMaterial, button);
            meshGO.transform.localScale = new Vector3(fScale, fScale, fScale);
            meshGO.transform.localRotation *= transform;
            MaterialUtil.DisableShadows(meshGO);

            standard_mat = new fMaterial(meshMaterial);
        }

        // creates a button with a background shape and a foreground mesh
        public void Create(Material bgMaterial, fMesh mesh, Material meshMaterial, float fScale, Frame3f deltaF)
        {
            button = new GameObject(UniqueNames.GetNext("HUDButton"));

            buttonMesh = AppendMeshGO("shape", HUDUtil.MakeBackgroundMesh(this.Shape), bgMaterial, button);
            buttonMesh.transform.Rotate(Vector3.right, -90.0f); // ??
            MaterialUtil.DisableShadows(buttonMesh);

            GameObject meshGO = AppendMeshGO("shape", mesh, meshMaterial, button);
            meshGO.transform.localScale = new Vector3(fScale, fScale, fScale);
            meshGO.transform.localPosition = deltaF.Origin;
            meshGO.transform.localRotation = deltaF.Rotation;
            MaterialUtil.DisableShadows(meshGO);

            standard_mat = new fMaterial(bgMaterial);
        }


        // events for clicked and double-clicked.
        // NOTE: for doubleclick you will always receive an [OnClicked,OnDoubleClicked] pair.
        //    (alternative would be to delay OnClicked...perhaps that should be added as an option)
        public event InputEventHandler OnClicked;
        public event InputEventHandler OnDoubleClicked;



        protected void update_material()
        {
            if ( Enabled == false && DisabledMaterial != null )
                MaterialUtil.SetMaterial(buttonMesh, DisabledMaterial);
            else
                MaterialUtil.SetMaterial(buttonMesh, StandardMaterial);
        }



        #region HUDStandardItem overrides

        protected override void OnEnabledChanged()
        {
            update_material();
        }

        #endregion



        #region SceneUIElement implementation

        override public fGameObject RootGameObject {
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
                    FUtil.SafeSendEvent(OnClicked, this, e);
                    sent_click = true;
                    last_click_time = FPlatform.RealTime();
                } else {
                    float delta = FPlatform.RealTime() - last_click_time;
                    if ( delta < SceneGraphConfig.ActiveDoubleClickDelay ) {
                        // if this second click comes fast enough, send doubleclick instead
                        FUtil.SafeSendEvent(OnDoubleClicked, this, e);
                        sent_click = false;
                        last_click_time = 0.0f;
                    } else {
                        // send single-click and reset timer
                        FUtil.SafeSendEvent(OnClicked, this, e);
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
            get {
                if (Shape.Type == HUDShapeType.Disc)
                    return new Vector2f(2 * Shape.Radius, 2 * Shape.Radius);
                else
                    return new Vector2f(Shape.Width, Shape.Height);
            }
        }

        public AxisAlignedBox2f Bounds2D { 
            get {
                Vector2f origin2 = RootGameObject.GetLocalPosition().xy;
                Vector2f sz = Size2D;
                return new AxisAlignedBox2f(origin2, sz.x / 2, sz.y / 2);
            }
        }


        #endregion
	}
}

