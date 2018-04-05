using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    /// <summary>
    /// Draw registered LineSet objects in the attached Camera.
    /// These are drawn per-frame using (Unity) GL commands.
    /// 
    /// Currently thick lines (ie non-1-pixel-wide) are drawn using
    /// quads, with no chamfers. So, they don't look great.
    /// 
    /// [TODO] pixel-width lines with sizes other than 1 are not supported.
    /// 
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UnityLineRender_GL : MonoBehaviour
    {
        Dictionary<fGameObject, LineSet> Sets = new Dictionary<fGameObject, LineSet>();

        public void AddLineSet(fGameObject source, LineSet set)
        {
            Util.gDevAssert(Sets.ContainsKey(source) == false);
            Sets[source] = set;
        }

        public bool RemoveLineSet(fGameObject source)
        {
            return Sets.Remove(source);
        }


        void Awake()
        {
            // load shaders
            DepthTestedMaterial = MaterialUtil.SafeLoadMaterial("StandardMaterials/line_shaders/gl_line_depth_test");
            OverlayMaterial = MaterialUtil.SafeLoadMaterial("StandardMaterials/line_shaders/gl_line_no_depth_test");
        }


        Material DepthTestedMaterial;
        Material OverlayMaterial;
        Vector3f CurrentCameraPos;



        public void OnPostRender()
        {
            CurrentCameraPos = Camera.main.transform.position;

            GL.PushMatrix();

            try {
                //Depth test lines
                DepthTestedMaterial.SetPass(0);
                draw_lines(true);

                //Lines without depth test
                OverlayMaterial.SetPass(0);
                draw_lines(false);
            } catch(Exception e) {
                DebugUtil.Log("UnityLineRenderer_GL: caught exception:" + e.Message);
            }

            GL.PopMatrix();
        }



        void draw_lines(bool depth_test)
        {
            foreach (var go_set in Sets) {
                fGameObject go = go_set.Key;
                if (go.IsDestroyed)
                    continue;
                LineSet lines = go_set.Value;

                if (go.IsVisible() == false)
                    continue;
                if (lines.DepthTest != depth_test)
                    continue;

                GL.PushMatrix();
                try {
                    Transform transform = ((GameObject)go).transform;
                    GL.MultMatrix(transform.localToWorldMatrix);
                    Vector3f localCam = transform.InverseTransformPoint(CurrentCameraPos);
                    if (lines.WidthType == LineWidthType.Pixel) {
                        if (lines.Width == 1)
                            draw_lines(lines, localCam);
                        else
                            draw_quads(lines, localCam);
                    } else {
                        draw_quads(lines, localCam);
                    }
                } catch {
                    throw;
                } finally {
                    GL.PopMatrix();
                }
            }
        }




        void draw_lines(LineSet lines, Vector3f cameraPos)
        {

            GL.Begin(GL.LINES);
            GL.Color(lines.Color);
            int NS = lines.Segments.Count;
            for ( int k = 0; k < NS; ++k) {
                Segment3d seg = lines.Segments[k];
                GL.Vertex((Vector3)seg.P0);
                GL.Vertex((Vector3)seg.P1);
            }
            GL.End();

            int NC = lines.Curves.Count;
            for ( int k = 0; k < NC; ++k ) {
                DCurve3 c = lines.Curves[k];
                GL.Begin(GL.LINE_STRIP);
                GL.Color(lines.Color);
                int NV = c.VertexCount;
                for (int i = 0; i < NV; ++i)
                    GL.Vertex((Vector3)c[i]);
                if (c.Closed)
                    GL.Vertex((Vector3)c[0]);
                GL.End();
            }
        }


        void draw_quads(LineSet lines, Vector3f cameraPos)
        {
            GL.Begin(GL.QUADS);
            GL.Color(lines.Color);

            int NS = lines.Segments.Count;
            for (int k = 0; k < NS; ++k) {
                Segment3d seg = lines.Segments[k];
                Vector3f start = (Vector3f)seg.P0, end = (Vector3f)seg.P1;
                if ( lines.UseFixedNormal )
                    draw_quad_fixednormal(ref start, ref end, ref lines.FixedNormal, lines.Width);
                else
                    draw_quad_viewalign(ref start, ref end, ref cameraPos, lines.Width);
            }

            int NC = lines.Curves.Count;
            for (int k = 0; k < NC; ++k) {
                DCurve3 c = lines.Curves[k];
                int NV = c.VertexCount;
                Vector3f prev = (Vector3f)c[0];
                for (int i = 1; i < NV; ++i) {
                    Vector3f cur = (Vector3f)c[i];
                    if (lines.UseFixedNormal)
                        draw_quad_fixednormal(ref prev, ref cur, ref lines.FixedNormal, lines.Width);
                    else
                        draw_quad_viewalign(ref prev, ref cur, ref cameraPos, lines.Width);
                    prev = cur;
                }
                if (c.Closed) {
                    Vector3f cur = (Vector3f)c[0];
                    if (lines.UseFixedNormal)
                        draw_quad_fixednormal(ref prev, ref cur, ref lines.FixedNormal, lines.Width);
                    else
                        draw_quad_viewalign(ref prev, ref cur, ref cameraPos, lines.Width);
                }
            }

            GL.End();

        }




        void draw_quad_viewalign(ref Vector3f start, ref Vector3f end, ref Vector3f cameraPos, float width)
        {
            Vector3f lineDir = end - start;
            Vector3f eyeDir = cameraPos - 0.5f * (start + end);
            lineDir.Normalize(); eyeDir.Normalize();
            Vector3f perp = lineDir.Cross(eyeDir);
            perp.Normalize();

            GL.Vertex(start - width * perp);
            GL.Vertex(start + width * perp);
            GL.Vertex(end + width * perp);
            GL.Vertex(end - width * perp);
        }



        void draw_quad_fixednormal(ref Vector3f start, ref Vector3f end, ref Vector3f normal, float width)
        {
            Vector3f lineDir = end - start;
            Vector3f perp = lineDir.UnitCross(normal);
            GL.Vertex(start - width * perp);
            GL.Vertex(start + width * perp);
            GL.Vertex(end + width * perp);
            GL.Vertex(end - width * perp);
        }


    }
}
