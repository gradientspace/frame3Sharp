using System;
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
            set { closed = value; if ( curve != null ) curve.Closed = closed; }
        }


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
            return (PolyCurveSO)BuildSO((curve) => {
                PolyCurveSO so = new PolyCurveSO() {
                    Curve = curve
                };
                so.Create(material);
                return so;
            }, material, scale);
        }


        public virtual TransformableSO BuildSO(Func<DCurve3,TransformableSO> SOBuilderF, SOMaterial material, float scale = 1.0f)
        {
            Vector3d vCenter = curve.GetBoundingBox().Center;
            DCurve3 shifted = new DCurve3(curve);
            for (int i = 0; i < shifted.VertexCount; ++i)
                shifted[i] -= vCenter;
            Frame3f shiftedFrame = new Frame3f((Vector3f)vCenter, Quaternionf.Identity);

            TransformableSO so = SOBuilderF(shifted);
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

        void update_geometry(FScene s)
        {
            if (bUpdatePending == false && curve_timestamp == curve.Timestamp)
                return;
            if (curve.VertexCount < 2)
                return;

            update_vertices(s);

            int N = (curve.Closed) ? curve.VertexCount+1 : curve.VertexCount;
            Vector3f[] vertices = new Vector3f[N];
            for (int i = 0; i < N; ++i)
                vertices[i] = (Vector3f)curve[i % curve.VertexCount];
            curveObject.SetVertices(vertices);

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
