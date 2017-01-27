using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public class HUDPopupMessage : HUDStandardItem
    {
        GameObject popup, popupMesh;

        public HUDShape Shape { get; set; }

        public HUDPopupMessage()
        {
            Shape = new HUDShape() { Type = HUDShapeType.Disc, Radius = 0.1f };
        }


        Mesh make_mesh()
        {
            if (Shape.Type == HUDShapeType.Disc) {
                return MeshGenerators.CreateTrivialDisc(Shape.Radius, Shape.Slices);
            } else if (Shape.Type == HUDShapeType.Rectangle) {
                return MeshGenerators.CreateTrivialRect(Shape.Width, Shape.Height,
                    Shape.UseUVSubRegion == true ?
                        MeshGenerators.UVRegionType.CenteredUVRectangle : MeshGenerators.UVRegionType.FullUVSquare);
            } else {
                throw new Exception("[HUDPopupMessage::make_mesh] unknown shape type!");
            }
        }

        // creates a popup in the desired geometry shape
        public void Create(Material defaultMaterial)
        {
            popup = new GameObject(UniqueNames.GetNext("HUDPopup"));
            popupMesh = AppendMeshGO("popup", make_mesh(),
                defaultMaterial, popup);

            popupMesh.transform.Rotate(Vector3.right, -90.0f); // ??
        }
   

        public event EventHandler OnDismissed;

        #region SceneUIElement implementation

        override public UnityEngine.GameObject RootGameObject
        {
            get { return popup; }
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
            if (IsGOHit(e.ray, popupMesh))
                FUtil.SafeSendEvent(OnDismissed, this, new EventArgs());
            return true;
        }

        #endregion
    }

}

