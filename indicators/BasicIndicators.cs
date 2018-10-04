using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class TextLabelIndicator : Indicator
    {
        fGameObject measureGO;
        fTextGameObject textGO;
        string curText = "";

        public fDimension TextHeight = fDimension.Scene(1.0f);

        public Func<bool> VisibleF = () => { return true; };
        public Func<Vector3f> ScenePositionF = () => { return Vector3f.Zero; };
        public Func<string> DimensionTextF = () => { return "1.0"; };

        public override fGameObject RootGameObject { get { return measureGO; } }
        public override bool IsVisible { get { return VisibleF(); } }
        public override CoordSpace InSpace { get { return CoordSpace.SceneCoords; } }

        public fMaterial material {
            get { return measureGO.GetMaterial();  }
            set { measureGO.SetMaterial(value); }
        }

        public TextLabelIndicator()
        {
        }

        public override void Setup()
        {
            measureGO = GameObjectFactory.CreateParentGO("dimension");
            measureGO.SetLayer(FPlatform.WidgetOverlayLayer);

            textGO = GameObjectFactory.CreateTextMeshGO("text", "1.0", Colorf.Black, TextHeight.SceneValuef);
            UnityUtil.AddChild(measureGO, textGO, false);
            textGO.SetLayer(FPlatform.WidgetOverlayLayer);
        }

        public override void PreRender()
        {
            Vector3f pos = ScenePositionF();
            measureGO.SetLocalPosition(pos);

            string sLabel = DimensionTextF();
            if (curText != sLabel) {
                textGO.SetText(sLabel);
                curText = sLabel;
            }

            textGO.SetHeight(TextHeight.SceneValuef);

            //float r = RadiusF();
            //sphereGO.SetLocalScale(r);
        }


        public override void Destroy() {
            GameObjectFactory.DestroyGO(measureGO);
        }
    }





    public class LineIndicator : Indicator
    {
        fLineGameObject lineGO;

        public fDimension LineWidth = fDimension.Scene(0.1f);

        public Func<bool> VisibleF = () => { return true; };
        public Func<Vector3f> SceneStartF = () => { return Vector3f.Zero; };
        public Func<Vector3f> SceneEndF = () => { return Vector3f.AxisY; };
        public Func<Colorf> ColorF = () => { return Colorf.Black; };
        public Func<int> LayerF = () => { return FPlatform.WidgetOverlayLayer; };

        public override fGameObject RootGameObject { get { return lineGO; } }
        public override bool IsVisible { get { return VisibleF(); } }
        public override CoordSpace InSpace { get { return CoordSpace.SceneCoords; } }

        public fMaterial material {
            get { return lineGO.GetMaterial();  }
            set { lineGO.SetMaterial(value); }
        }

        public LineIndicator()
        {
        }

        public override void Setup()
        {
            lineGO = GameObjectFactory.CreateLineGO("dimension_line", ColorF(), LineWidth.WorldValuef, LineWidthType.World);
            lineGO.SetLayer(LayerF());
        }

        public override void PreRender()
        {
            lineGO.SetStart(SceneStartF());
            lineGO.SetEnd(SceneEndF());
            lineGO.SetColor(ColorF());
            // line width is set in world units!
            lineGO.SetLineWidth(LineWidth.WorldValuef);
            lineGO.SetLayer(LayerF(), true);
        }


        public override void Destroy() {
            GameObjectFactory.DestroyGO(lineGO);
        }
    }





    public class CircleIndicator : Indicator
    {
        fCircleGameObject circleGO;

        public fDimension LineWidth = fDimension.Scene(0.1f);

        public Func<bool> VisibleF = () => { return true; };
        public Func<Frame3f> SceneFrameF = () => { return Frame3f.Identity; };
        public Func<float> RadiusF = () => { return 1.0f; };
        public Func<Colorf> ColorF = () => { return Colorf.Black; };


        public override fGameObject RootGameObject { get { return circleGO; } }
        public override bool IsVisible { get { return VisibleF(); } }
        public override CoordSpace InSpace { get { return CoordSpace.SceneCoords; } }

        public fMaterial material {
            get { return circleGO.GetMaterial();  }
            set { circleGO.SetMaterial(value); }
        }

        public CircleIndicator()
        {
        }

        public override void Setup()
        {
            circleGO = GameObjectFactory.CreateCircleGO("dimension_circle", RadiusF(), ColorF(), LineWidth.WorldValuef, LineWidthType.World);
            circleGO.SetLayer(FPlatform.WidgetOverlayLayer);
        }

        public override void PreRender()
        {
            Frame3f f = SceneFrameF();
            circleGO.SetLocalFrame(f);

            circleGO.SetRadius(RadiusF());
            circleGO.SetColor(ColorF());
            circleGO.SetLineWidth(LineWidth.WorldValuef);
        }


        public override void Destroy() {
            GameObjectFactory.DestroyGO(circleGO);
        }
    }






    public class SectionPlaneIndicator : Indicator
    {
        fGameObject planeGO;
        fMaterial planeMaterial;

        public fDimension Width = fDimension.Scene(1.0f);


        public Func<bool> VisibleF = () => { return true; };
        public Func<Frame3f> SceneFrameF = () => { return Frame3f.Identity; };
        public Func<fMaterial> MaterialF = () => { return MaterialUtil.CreateTransparentMaterialF(Colorf.Green, 0.5f); };
        public Func<Colorf> ColorF = () => { return new Colorf(Colorf.LightGreen, 0.5f); };

        public override fGameObject RootGameObject { get { return planeGO; } }
        public override bool IsVisible { get { return VisibleF(); } }
        public override CoordSpace InSpace { get { return CoordSpace.SceneCoords; } }

        public fMaterial material {
            get { return planeMaterial;  }
            set { planeMaterial = value;  planeGO.SetMaterial(planeMaterial); }
        }

        public SectionPlaneIndicator()
        {
        }

        public override void Setup()
        {
            planeGO = GameObjectFactory.CreateMeshGO("section_plane", UnityUtil.GetTwoSidedPlaneMesh(), false);
            planeMaterial = MaterialF();
            planeGO.SetMaterial(planeMaterial);
            planeMaterial.color = ColorF();
            MaterialUtil.DisableShadows(planeGO);
            //planeGO.SetLayer(FPlatform.WidgetOverlayLayer);
        }

        public override void PreRender()
        {
            Frame3f f = SceneFrameF();
            planeGO.SetLocalFrame(f);
            planeGO.SetLocalScale( Width.SceneValuef * Vector3f.One );
            planeMaterial.color = ColorF();
        }


        public override void Destroy() {
            GameObjectFactory.DestroyGO(planeGO);
        }
    }





    public class SphereIndicator : Indicator
    {
        fGameObject sphereGO;
        fMaterial sphereMaterial;

        public fDimension Radius = fDimension.Scene(0.2f);

        public Func<bool> VisibleF = () => { return true; };
        public Func<Frame3f> SceneFrameF = () => { return Frame3f.Identity; };
        public Func<fMaterial> MaterialF = () => { return MaterialUtil.CreateDynamicTransparencyMaterialF(Colorf.Green); };
        public Func<Colorf> ColorF = () => { return Colorf.ForestGreen; };

        public override fGameObject RootGameObject { get { return sphereGO; } }
        public override bool IsVisible { get { return VisibleF(); } }
        public override CoordSpace InSpace { get { return CoordSpace.SceneCoords; } }

        public fMaterial material {
            get { return sphereMaterial; }
            set { sphereMaterial = value; sphereGO.SetMaterial(sphereMaterial); }
        }

        public SphereIndicator()
        {
        }

        public override void Setup()
        {
            sphereGO = GameObjectFactory.CreateMeshGO("sphere", UnityUtil.GetSphereMesh(), false);
            sphereMaterial = MaterialF();
            sphereGO.SetMaterial(sphereMaterial);
            sphereMaterial.color = ColorF();
            MaterialUtil.DisableShadows(sphereGO);
        }

        public override void PreRender()
        {
            Frame3f frame = SceneFrameF();
            sphereGO.SetLocalFrame(frame);
            sphereMaterial.color = ColorF();
            sphereGO.SetLocalScale(2 * Radius.SceneValuef);
        }


        public override void Destroy()
        {
            GameObjectFactory.DestroyGO(sphereGO);
        }
    }



}
