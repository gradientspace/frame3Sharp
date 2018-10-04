using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class BrushCursorSphere : Indicator
    {
        fGameObject sphereGO;

        public fDimension Radius = fDimension.World(1.0f);

        public Func<bool> VisibleF = () => { return true; };
        public Func<Vector3f> PositionF = () => { return Vector3f.Zero; };


        public override fGameObject RootGameObject { get { return sphereGO; } }
        public override bool IsVisible { get { return VisibleF(); } }
        public override CoordSpace InSpace { get { return CoordSpace.WorldCoords; } }


        public fMaterial material {
            get { return sphereGO.GetMaterial();  }
            set { sphereGO.SetMaterial(value); }
        }

        public fMaterial sharedMaterial {
            get { return sphereGO.GetMaterial(); }
            set { sphereGO.SetMaterial(value, true); }
        }

        public BrushCursorSphere()
        {
        }

        public override void Setup()
        {
            sphereGO = GameObjectFactory.CreateMeshGO("brush_roi_sphere");
            sphereGO.SetMesh(UnityUtil.GetSphereMesh());
            sphereGO.SetLayer(FPlatform.WidgetOverlayLayer);
        }

        public override void PreRender()
        {
            Vector3f pos = PositionF();
            sphereGO.SetPosition(pos);
            sphereGO.SetLocalScale( 2 * Radius.WorldValuef );
        }


        public override void Destroy() {
            GameObjectFactory.DestroyGO(sphereGO);
        }
    }
}
