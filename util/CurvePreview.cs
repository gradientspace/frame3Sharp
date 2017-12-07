using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    // NOTE: assumption in various places is that vertices of Curve are
    //   stored in Scene coordinates !!
    public class CurvePreview
    {
        DCurve3 curve;
        public DCurve3 Curve
        {
            get { return curve; }
            //set { curve = value; bUpdatePending = true; }
        }
        public bool CurveModified
        {
            get { return bUpdatePending; }
            set { if ( value == true ) bUpdatePending = true; }
        }
        int curve_timestamp = -1;

        bool closed = false;
        public bool Closed
        {
            get { return closed; }
            set { if (closed != value) { closed = value; if (curve != null) curve.Closed = closed; bUpdatePending = true; } }
        }


        /// <summary>
        /// You can use this to do processing of the curve before display. For exmaple you
        /// might want to actually display a smoothed version of the curve, but you can't just
        /// smooth it every frame, you need to re-smooth the input version each frame.
        /// You can do that by just doing a smoothing pass with this function, it will be applied 
        /// to the input curve whenever it is updated, and in the Create() function
        /// </summary>
        public Action<List<Vector3d>> CurveProcessorF = null;



        fPolylineGameObject curveObject;
        bool bUpdatePending;

        public void Create(SOMaterial useMaterial, fGameObject parent, int nLayer = -1)
        {
            if (curve == null)
                curve = new DCurve3() { Closed = closed };

            curveObject = GameObjectFactory.CreatePolylineGO("preview_curve", null, useMaterial.RGBColor, 0.05f, LineWidthType.World);
            if (nLayer >= 0)
                curveObject.SetLayer(nLayer);

            bUpdatePending = true;

            parent.AddChild(curveObject, false);
        }

        public void PreRender(FScene s)
        {
            update_geometry(s);
        }


        public virtual int VertexCount
        {
            get { return curve.VertexCount; }
        }
        public virtual void AppendVertex(Vector3d v)
        {
            curve.AppendVertex(v);
        }
        public virtual Vector3d Tangent(int i)
        {
            return curve.Tangent(i);
        }
        public virtual Vector3d this[int i]
        {
            get { return curve[i]; }
            set { curve[i] = value; }
        }



        public virtual PolyCurveSO BuildSO(SOMaterial material, float scale = 1.0f)
        {
            return (PolyCurveSO)BuildSO((curveIn) => {
                PolyCurveSO so = new PolyCurveSO() {
                    Curve = curveIn
                };
                so.Create(material);
                return so;
            }, material, scale);
        }


        public virtual SceneObject BuildSO(Func<DCurve3,SceneObject> SOBuilderF, SOMaterial material, float scale = 1.0f)
        {
            // create shifted curve
            Vector3d vCenter = curve.GetBoundingBox().Center;
            DCurve3 shifted = bake_transform(-vCenter);
            Frame3f shiftedFrame = new Frame3f((Vector3f)vCenter, Quaternionf.Identity);

            SceneObject so = SOBuilderF(shifted);
            so.SetLocalFrame(shiftedFrame, CoordSpace.WorldCoords);

            return so;
        }



        public void Destroy()
        {
            curveObject.Destroy();
        }


        protected virtual void update_vertices(FScene s)
        {
            // this is here so subclasses can override
        }



        List<Vector3d> buffer = new List<Vector3d>(64);
        List<Vector3f> verticesf = new List<Vector3f>(64);



        DCurve3 bake_transform(Vector3d vShift)
        {
            buffer.Clear();
            for (int i = 0; i < curve.VertexCount; ++i) {
                buffer.Add(curve[i]);
            }
            if (CurveProcessorF != null)
                CurveProcessorF(buffer);
            for (int i = 0; i < buffer.Count; ++i)
                buffer[i] += vShift;
            return new DCurve3(buffer, curve.Closed);
        }


        void update_geometry(FScene s)
        {
            if (bUpdatePending == false && curve_timestamp == curve.Timestamp)
                return;
            if (curve.VertexCount < 2)
                return;

            update_vertices(s);

            verticesf.Clear();
            buffer.Clear();

            for ( int i = 0; i < curve.VertexCount; ++i ) {
                buffer.Add(curve[i % curve.VertexCount]);
            }
            if (CurveProcessorF != null)
                CurveProcessorF(buffer);

            int Nmod = buffer.Count;
            int N = (curve.Closed) ? buffer.Count + 1 : buffer.Count;
            for (int i = 0; i < N; ++i)
                verticesf.Add((Vector3f)buffer[i % Nmod]);
            curveObject.SetVertices(verticesf);

            float fWidth = VRUtil.EstimateStableCurveWidth(s, Frame3f.Identity, curve,
                SceneGraphConfig.DefaultSceneCurveVisualDegrees);
            if (fWidth > 0) {
                curveObject.SetLineWidth(fWidth);
            }

            bUpdatePending = false;
            curve_timestamp = curve.Timestamp;
        }



    }
}
