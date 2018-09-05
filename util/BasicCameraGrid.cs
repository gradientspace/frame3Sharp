using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    /// <summary>
    /// Simple static grid that faces camera
    /// </summary>
    public class BasicCameraGrid : BaseSceneUIElement
    {
        public float StepSize {
            get { return step_size; }
            set { if (MathUtil.EpsilonEqual(step_size, value) == false) { step_size = value; grids_valid = false; } }
        }
        float step_size = 1.0f;


        public Vector3f Center {
            get { return center; }
            set { center = value; }
        }
        Vector3f center = Vector3f.Zero;


        Vector2i axis_steps = new Vector2i(256, 128);

        public Colorf Color = Colorf.DimGrey;

        fLineSetGameObject grid_xy = null;
        bool grids_valid = false;
        float alpha_xy = 1;

        public BasicCameraGrid(string name = "basic_grid") : base(name)
        {
            grid_xy = new fLineSetGameObject("lines_xy");
            RootGameObject.AddChild(grid_xy, false);
        }

        public Frame3f GetFrame() { return RootGameObject.GetLocalFrame(); }
        public void SetFrame(Frame3f frame) { RootGameObject.SetLocalFrame(frame); }

        public override void PreRender() {
            fCamera cam = Parent.Context.ActiveCamera;
            float zNear = 2 * cam.NearClipPlane;
            Vector3f forwardW = cam.Forward();
            Vector3f gridCenterW = cam.GetPosition() + zNear * forwardW;

            Vector3f inCenterW = (Parent as FScene).ToWorldP(Center);

            //Vector3f gridDirS = (Parent as FScene).ToSceneN(forwardW);

            //Frame3f gridFrame = new Frame3f(gridCenter, gridDir);
            //Frame3f gridFrame = new Frame3f(Center, gridDirS);

            //Frame3f gridFrameW = new Frame3f(inCenterW, forwardW);
            Frame3f gridFrameW = new Frame3f(gridCenterW, forwardW);
            RootGameObject.SetWorldFrame(gridFrameW);

            if (grids_valid == false) 
                update_grids();
        }



        public override bool IsVisible {
            get { return RootGameObject.IsVisible(); }
            set {
                RootGameObject.SetVisible(value);
                UpdateVisibility();
            }
        }


        public override void SetLayer(int nLayer)
        {
            base.SetLayer(nLayer);
            // [RMS] have to do this explicitly because currently the children are
            // GameObject, not fGameObject, so the fGameObject.SetLayer override will
            // not be called!
            if (grid_xy != null) grid_xy.SetLayer(nLayer, true);
        }



        public void UpdateColor(Colorf color)
        {
            Color = color;
            update_color();
        }
        void update_color()
        {
            if (grid_xy != null) grid_xy.Lines.Color = new Colorf(Color, alpha_xy*Color.a);
        }


        public void UpdateVisibility()
        {
            if (grid_xy != null) grid_xy.SetVisible(IsVisible);
        }



        void update_grids()
        {
            if (grid_xy != null) {
                grid_xy.SafeUpdateLines((lines) => {
                    lines.Segments.Clear();
                    generate_grid_lines(lines, Vector3f.AxisX, Vector3f.AxisY, axis_steps.x, axis_steps.y, step_size);
                });
                update_color();
            }
            grids_valid = true;
        }



        void generate_grid_lines(LineSet lines, Vector3f axis1, Vector3f axis2, int nExtentCount1, int nExtentCount2, float stepSize)
        {
            Vector2f minPos = stepSize * new Vector2f(-nExtentCount1, -nExtentCount2);
            Vector2f maxPos = stepSize * new Vector2f(nExtentCount1, nExtentCount2);
            int n1 = 2 * nExtentCount1, n2 = 2 * nExtentCount2;

            bool IncludeEnds = false;
            int start1 = (IncludeEnds) ? 0 : 1, end1 = (IncludeEnds) ? n1 : n1 - 1;
            for ( int i1 = start1; i1 <= end1; i1++ ) {
                float t = (float)i1 / (float)n1;
                float tx = MathUtil.Lerp(minPos.x, maxPos.x, t);
                Vector3f p0 = tx*axis1 + minPos.y*axis2;
                Vector3f p1 = tx*axis1 + maxPos.y*axis2;
                lines.Segments.Add(new Segment3d(p0, p1));
            }

            int start2 = (IncludeEnds) ? 0 : 1, end2 = (IncludeEnds) ? n2 : n2 - 1;
            for (int i2 = start2; i2 <= end2; i2++) {
                float t = (float)i2 / (float)n2;
                float ty = MathUtil.Lerp(minPos.y, maxPos.y, t);
                Vector3f p0 = minPos.x * axis1 + ty * axis2;
                Vector3f p1 = maxPos.x * axis1 + ty * axis2;
                lines.Segments.Add(new Segment3d(p0, p1));
            }
        }



    }
}
