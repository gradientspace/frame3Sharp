using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;

namespace f3
{

    /// <summary>
    /// Simple static grid on x/y/z planes, supports various dynamic-visibility modes
    /// </summary>
    public class BasicSceneGrid : BaseSceneUIElement
    {

        /// <summary> Number of steps in each direction along each axis (ie actual steps is 2*K+1) </summary>
        public Vector3i AxisSteps {
            get { return axis_steps; }
            set { if (axis_steps != value) { axis_steps = value; grids_valid = false; } }
        }
        Vector3i axis_steps = new Vector3i(25, 25, 25);

        public float StepSize {
            get { return step_size; }
            set { if (MathUtil.EpsilonEqual(step_size, value) == false) { step_size = value; grids_valid = false; } }
        }
        float step_size = 1.0f;


        public enum GridVisibilityModes
        {
            AllVisible,
            MaxVisibleOne,
            MaxVisibleTwo,
            DynamicFade
        }
        public GridVisibilityModes VisibilityMode = GridVisibilityModes.DynamicFade;

        public bool IncludeEnds = false;
        public float AlphaCutoff = 0.25f;

        public Colorf ColorXY = Colorf.DimGrey;
        public Colorf ColorYZ = Colorf.DimGrey;
        public Colorf ColorXZ = Colorf.DimGrey;


        fLineSetGameObject grid_xy = null;
        fLineSetGameObject grid_yz = null;
        fLineSetGameObject grid_xz = null;
        bool grids_valid = false;
        float alpha_xy = 1, alpha_yz = 1, alpha_xz = 1;

        public BasicSceneGrid(bool xyPlane, bool yzPlane, bool xzPlane, string name = "basic_grid") : base(name)
        {
            grid_xy = (xyPlane) ? new fLineSetGameObject("lines_x") : null;
            grid_yz = (yzPlane) ? new fLineSetGameObject("lines_y") : null;
            grid_xz = (xzPlane) ? new fLineSetGameObject("lines_z") : null;
            if (grid_xy != null) RootGameObject.AddChild(grid_xy, false);
            if (grid_yz != null) RootGameObject.AddChild(grid_yz, false);
            if (grid_xz != null) RootGameObject.AddChild(grid_xz, false);
        }

        public Frame3f GetFrame() { return RootGameObject.GetLocalFrame(); }
        public void SetFrame(Frame3f frame) { RootGameObject.SetLocalFrame(frame); }


        Vector3f cachedCamDir = Vector3f.Zero;

        public override void PreRender() {
            if (grids_valid ==false) 
                update_grids();

            Vector3f camDirW = Parent.Context.ActiveCamera.Forward();
            Vector3f sceneCamDir = Parent.Context.Scene.ToSceneN(camDirW);
            if (cachedCamDir.EpsilonEqual(sceneCamDir, MathUtil.ZeroTolerancef) == false) {
                cachedCamDir = sceneCamDir;
                UpdateVisibility(sceneCamDir);
            }
        }



        public override bool IsVisible {
            get { return RootGameObject.IsVisible(); }
            set {
                RootGameObject.SetVisible(value);
                cachedCamDir = Vector3f.Zero;
                UpdateVisibility(Vector3f.Zero, true);
            }
        }




        public void UpdateColor(Colorf color)
        {
            ColorXY = ColorYZ = ColorXZ = color;
            update_color();
        }
        void update_color()
        {
            if (grid_xy != null) grid_xy.Lines.Color = new Colorf(ColorXY, alpha_xy*ColorXY.a);
            if (grid_yz != null) grid_yz.Lines.Color = new Colorf(ColorYZ, alpha_yz*ColorYZ.a);
            if (grid_xz != null) grid_xz.Lines.Color = new Colorf(ColorXZ, alpha_xz*ColorXZ.a);
        }


        public void UpdateVisibility(Vector3f vVisibilityDir, bool bHideShowOnly = false)
        {
            if (IsVisible == false) {
                if (grid_xy != null) grid_xy.SetVisible(false);
                if (grid_yz != null) grid_yz.SetVisible(false);
                if (grid_xz != null) grid_xz.SetVisible(false);
                return;
            }

            if (grid_xy != null) grid_xy.SetVisible(true);
            if (grid_yz != null) grid_yz.SetVisible(true);
            if (grid_xz != null) grid_xz.SetVisible(true);
            if (VisibilityMode == GridVisibilityModes.AllVisible || bHideShowOnly)
                return;

            if (VisibilityMode == GridVisibilityModes.DynamicFade) {
                UpdateVisibility_Fade(vVisibilityDir);
            } else {
                UpdateVisibility_Snapped(vVisibilityDir);
            }
        }


        public void UpdateVisibility_Fade(Vector3f vVisibilityDir)
        {
            if (grid_xy != null) grid_xy.SetVisible(true);
            if (grid_yz != null) grid_yz.SetVisible(true);
            if (grid_xz != null) grid_xz.SetVisible(true);

            alpha_xy = (grid_xy != null) ? Math.Abs(vVisibilityDir.Dot(Vector3f.AxisZ)) : -9999;
            alpha_yz = (grid_yz != null) ? Math.Abs(vVisibilityDir.Dot(Vector3f.AxisX)) : -9999;
            alpha_xz = (grid_xz != null) ? Math.Abs(vVisibilityDir.Dot(Vector3f.AxisY)) : -9999;
            alpha_xy = dot_to_alpha(alpha_xy);
            alpha_yz = dot_to_alpha(alpha_yz);
            alpha_xz = dot_to_alpha(alpha_xz);
            update_color();
        }
        float dot_to_alpha(float dot)
        {
            float f = MathUtil.WyvillRise01(dot);
            if (f < AlphaCutoff) f = 0;
            return f;
        }




        public void UpdateVisibility_Snapped(Vector3f vVisibilityDir)
        {
            if (grid_xy != null) grid_xy.SetVisible(false);
            if (grid_yz != null) grid_yz.SetVisible(false);
            if (grid_xz != null) grid_xz.SetVisible(false);

            float dot_xy = (grid_xy != null) ? Math.Abs(vVisibilityDir.Dot(Vector3f.AxisZ)) : -9999;
            float dot_yz = (grid_yz != null) ? Math.Abs(vVisibilityDir.Dot(Vector3f.AxisX)) : -9999;
            float dot_xz = (grid_xz != null) ? Math.Abs(vVisibilityDir.Dot(Vector3f.AxisY)) : -9999;

            float largest_dot = MathUtil.Max(dot_xy, dot_yz, dot_xz);
            float o1, o2;
            if (largest_dot == dot_xy) {
                grid_xy.SetVisible(true); o1 = dot_yz; o2 = dot_xz;
            } else if (largest_dot == dot_yz) {
                grid_yz.SetVisible(true); o1 = dot_xy; o2 = dot_xz;
            } else {
                grid_xz.SetVisible(true); o1 = dot_xy; o2 = dot_yz;
            }

            if ( VisibilityMode == GridVisibilityModes.MaxVisibleTwo ) {
                float second_largest = Math.Max(o1, o2);
                if (grid_xy != null && grid_xy.IsVisible() == false && second_largest == dot_xy)
                    grid_xy.SetVisible(true);
                else if (grid_xz != null && grid_xz.IsVisible() == false && second_largest == dot_xz)
                    grid_xz.SetVisible(true);
                else if ( grid_yz != null )
                    grid_yz.SetVisible(true);
            }
        }



        void update_grids()
        {
            if (grid_xy != null) {
                grid_xy.SafeUpdateLines((lines) => {
                    lines.Segments.Clear();
                    generate_grid_lines(lines, Vector3f.AxisX, Vector3f.AxisY, axis_steps.x, axis_steps.y, step_size);
                });
            }
            if (grid_yz != null) {
                grid_yz.SafeUpdateLines((lines) => {
                    lines.Segments.Clear();
                    generate_grid_lines(lines, Vector3f.AxisY, Vector3f.AxisZ, axis_steps.y, axis_steps.z, step_size);
                });
            }
            if (grid_xz != null) {
                grid_xz.SafeUpdateLines((lines) => {
                    lines.Segments.Clear();
                    generate_grid_lines(lines, Vector3f.AxisX, Vector3f.AxisZ, axis_steps.x, axis_steps.z, step_size);
                });
            }

            grids_valid = true;
        }



        void generate_grid_lines(LineSet lines, Vector3f axis1, Vector3f axis2, int nExtentCount1, int nExtentCount2, float stepSize)
        {
            Vector2f minPos = stepSize * new Vector2f(-nExtentCount1, -nExtentCount2);
            Vector2f maxPos = stepSize * new Vector2f(nExtentCount1, nExtentCount2);
            int n1 = 2 * nExtentCount1, n2 = 2 * nExtentCount2;

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
