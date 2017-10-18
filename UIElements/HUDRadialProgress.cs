using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class HUDRadialProgress : HUDProgressBase, IBoxModelElement
    {
        // should go in subclass...
        fDiscGameObject backgroundGO;
        fDiscGameObject progressGO;

        public override void Create()
        {
            base.Create();

            backgroundGO = GameObjectFactory.CreateDiscGO(rootGO.GetName() + "_bg",
                radius, bgColor, true);
            MaterialUtil.DisableShadows(backgroundGO);
            backgroundGO.RotateD(Vector3f.AxisX, -90.0f);   // make vertical
            AppendNewGO(backgroundGO, rootGO, false);

            progressGO = GameObjectFactory.CreateDiscGO(rootGO.GetName() + "_progress",
                radius, completedColor, true);
            progressGO.SetStartAngleDeg(89.0f);
            progressGO.SetEndAngleDeg(90.0f);

            MaterialUtil.DisableShadows(progressGO);
            progressGO.RotateD(Vector3f.AxisX, -90.0f);   // make vertical
            progressGO.Translate(0.001f * Vector3f.AxisY, true);
            AppendNewGO(progressGO, rootGO, false);

            update_geometry();
        }


        float radius = 1;
        Colorf bgColor = Colorf.White;
        Colorf completedColor = Colorf.BlueMetal;

        public float Radius {
            get { return radius; }
            set {
                if (radius != value) {
                    radius = value;
                    update_geometry();
                }
            }
        }

        public Colorf BackgroundColor
        {
            get { return bgColor; }
            set {
                if (bgColor != value) {
                    bgColor = value;
                    backgroundGO.SetColor(bgColor);
                }
            }
        }


        public Colorf CompletedColor
        {
            get { return completedColor; }
            set {
                if (completedColor != value) {
                    completedColor = value;
                    progressGO.SetColor(completedColor);
                }
            }
        }


        protected override void update_geometry()
        {
            if (rootGO == null)
                return;

            (backgroundGO as fDiscGameObject).SetRadius(radius);
            (progressGO as fDiscGameObject).SetRadius(0.95f*radius);

            double fT = Progress / MaxProgress;
            progressGO.SetStartAngleDeg(90 - MathUtil.Clamp((float)(360 * fT), 1.0f, 359.999f));
        }




       #region IBoxModelElement implementation


        public Vector2f Size2D {
            get {
                return new Vector2f(2 * Radius, 2 * Radius);
            }
        }

        public AxisAlignedBox2f Bounds2D { 
            get {
                return new AxisAlignedBox2f(Vector2f.Zero, Radius, Radius);
            }
        }


        #endregion


    }
}
