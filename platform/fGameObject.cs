using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    public interface CurveRendererImplementation
    {
        void initialize(fGameObject go, Colorf color);
        void update_curve(Vector3f[] Vertices);
        void update_num_points(int N);
        void update_position(int i, Vector3f v);
        void update_width(float width);
        void update_color(Colorf color);
    }
    public interface CurveRendererFactory
    {
        CurveRendererImplementation Build();
    }



    [Flags]
    public enum FGOFlags
    {
        NoFlags = 0,
        EnablePreRender = 1
    }



    //
    // fGameObject wraps a GameObject for frame3Sharp. The idea is that eventually we
    //  will be able to "replace" GameObject with something else, ie non-Unity stuff.
    //
    // implicit cast operators allow transparent conversion between GameObject and fGameObject
    //
    public class fGameObject
    {
        protected GameObject go;

        public fGameObject(GameObject go, FGOFlags flags = FGOFlags.NoFlags)
        {
            Initialize(go, flags);
        }

        /// <summary>
        /// If you use parameterless constructor, you really should call Initialize() ASAP!
        /// </summary>
        public fGameObject()
        {
            this.go = null;
        }


        public virtual void Initialize(GameObject go, FGOFlags flags)
        {
            this.go = go;

            bool bEnablePreRender = (flags & FGOFlags.EnablePreRender) != 0;
            if (bEnablePreRender) {
                if (go.GetComponent<PreRenderBehavior>() != null)
                    throw new Exception("fGameObject.Initialize: tried to add PreRenderBehavior to this go, but already exists!");
                PreRenderBehavior pb = go.AddComponent<PreRenderBehavior>();
                pb.ParentFGO = this;
            }
        }



        public virtual void Destroy() {
            if (go != null) {
                GameObject.Destroy(go);
            }
        }



        // allow game object wrapper to do things here, eg lazy updates, etc
        // This will be called by the GameObject itself, using the PreRenderBehavior (!)
        public virtual void PreRender()
        {
        }


        public virtual void SetName(string name)
        {
            go.name = name;
        }
        public virtual string GetName()
        {
            return go.name;
        }

        public virtual void SetLayer(int layer)
        {
            go.layer = layer;
        }
        public virtual int GetLayer()
        {
            return go.layer;
        }

        public virtual bool HasChildren()
        {
            return go.transform.childCount > 0;
        }
        public virtual IEnumerable<GameObject> Children()
        {
            for (int k = 0; k < go.transform.childCount; ++k)
                yield return go.transform.GetChild(k).gameObject;
        }
        public virtual void AddChild(fGameObject child, bool bKeepWorldPosition = true)
        {
            child.go.transform.SetParent(go.transform, bKeepWorldPosition);
        }


        public virtual bool IsSameOrChild(fGameObject testGO)
        {
            if (this == testGO)
                return true;
            foreach ( GameObject childGO in go.Children() ) { 
                if (childGO.IsSameOrChild(testGO))
                    return true;
            }
            return false;
        }
        public virtual bool IsSameOrChild(GameObject testGO)
        {
            if (this.go == testGO)
                return true;
            foreach ( GameObject childGO in go.Children() ) { 
                if (childGO.IsSameOrChild(testGO))
                    return true;
            }
            return false;
        }


        public virtual Mesh GetMesh()
        {
            return go.GetComponent<MeshFilter>().mesh;
        }
        public virtual Mesh GetSharedMesh()
        {
            return go.GetComponent<MeshFilter>().sharedMesh;
        }
        public virtual void SetMesh(Mesh m)
        {
            go.GetComponent<MeshFilter>().mesh = m;
        }
        public virtual void SetSharedMesh(Mesh m)
        {
            go.GetComponent<MeshFilter>().sharedMesh = m;
        }

        public virtual void SetMaterial(fMaterial mat, bool bShared = false)
        {
            go.SetMaterial(mat, bShared);
        }
        public virtual fMaterial GetMaterial()
        {
            return new fMaterial(go.GetMaterial());
        }

        public virtual void SetColor(Colorf color)
        {
            Renderer r = go.GetComponent<Renderer>();
            r.material.color = color;
        }


        public void SetParent(fGameObject parentGO, bool bKeepWorldPosition = false)
        {
            if (parentGO == null)
                go.transform.parent = null;
            else
                go.transform.SetParent(((GameObject)parentGO).transform, bKeepWorldPosition);
        }



        public void Hide()
        {
            go.Hide();
        }
        public void Show()
        {
            go.Show();
        }
        public void SetVisible(bool bVisible)
        {
            go.SetVisible(bVisible);
        }
        public bool IsVisible()
        {
            return go.IsVisible();
        }


        public void SetActive(bool bActive)
        {
            go.SetActive(bActive);
        }





        public virtual void SetPosition(Vector3f vPosition)
        {
            go.transform.position = vPosition;
        }
        public virtual Vector3f GetPosition()
        {
            return go.transform.position;
        }

        public virtual void SetRotation(Quaternionf rotation)
        {
            go.transform.rotation = rotation;
        }
        public virtual Quaternionf GetRotation()
        {
            return go.transform.rotation;
        }


        public virtual void SetLocalPosition(Vector3f vPosition)
        {
            go.transform.localPosition = vPosition;
        }
        public virtual Vector3f GetLocalPosition()
        {
            return go.transform.localPosition;
        }

        public virtual void SetLocalRotation(Quaternionf rotation)
        {
            go.transform.localRotation = rotation;
        }
        public virtual Quaternionf GetLocalRotation()
        {
            return go.transform.localRotation;
        }


        public virtual void SetLocalScale(Vector3f vScale)
        {
            go.transform.localScale = vScale;
        }
        public virtual void SetLocalScale(float fScale)
        {
            go.transform.localScale = fScale * Vector3f.One; 
        }
        public virtual Vector3f GetLocalScale()
        {
            return go.transform.localScale;
        }


        public virtual Frame3f GetWorldFrame() {
            return UnityUtil.GetGameObjectFrame(go, CoordSpace.WorldCoords);
        }
        public virtual void SetWorldFrame(Frame3f f) {
            UnityUtil.SetGameObjectFrame(go, f, CoordSpace.WorldCoords);
        }

        public virtual Frame3f GetLocalFrame() {
            return UnityUtil.GetGameObjectFrame(go, CoordSpace.ObjectCoords);
        }
        public virtual void SetLocalFrame(Frame3f f) {
            UnityUtil.SetGameObjectFrame(go, f, CoordSpace.ObjectCoords);
        }


        public virtual Vector3f PointToWorld(Vector3f local) {
            return go.transform.TransformPoint(local);
        }
        public virtual Vector3f PointToLocal(Vector3f world) {
            return go.transform.InverseTransformPoint(world);
        }


        public virtual void RotateD(Vector3f axis, float fAngleDeg)
        {
            go.transform.Rotate(axis, fAngleDeg);
        }
        public virtual void RotateAroundD(Vector3f point, Vector3f axis, float fAngleDeg)
        {
            go.transform.RotateAround(point, axis, fAngleDeg);
        }

        // 05/09/2017 [RMS] just discovered that transform.Translate() default behavior
        // is to translate in current-orientation axes (!). Explains a lot of weirdness.
        // Exposed this as parameter, forcing callers to specify it for now.
        public virtual void Translate(Vector3f translation, bool bFrameAxes)
        {
            go.transform.Translate(translation, bFrameAxes ? Space.Self : Space.World  );
        }



        // what to do about this??
        public virtual T AddComponent<T>() where T : Component
        {
            T comp = go.AddComponent<T>();
            return comp;
        }
        public virtual T GetComponent<T>() where T : Component
        {
            return go.GetComponent<T>();
        }


        public static implicit operator UnityEngine.GameObject(fGameObject go)
        {
            return (go != null) ? go.go : null;
        }
        public static implicit operator fGameObject(UnityEngine.GameObject go)
        {
            return (go != null) ? new fGameObject(go, FGOFlags.NoFlags) : null;
        }
    }






    public class fTextGameObject : fGameObject
    {
        Vector2f size;
        fText textObj;

        public fTextGameObject(GameObject go, fText textObj, Vector2f size)
            : base(go, FGOFlags.EnablePreRender)
        {
            this.size = size;
            this.textObj = textObj;
        }

        public fText TextObject
        {
            get { return textObj; }
        }

        public Vector2f GetSize() {
            return size;
        }

        public void SetHeight(float fNewHeight)
        {
            // doesn't support textmeshpro text...

            TextMesh tm = go.GetComponent<TextMesh>();
            tm.transform.localScale = Vector3f.One;
            Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
            float fScaleH = fNewHeight / size.y;
            tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
            size = new Vector2f(fScaleH * size.x, fNewHeight);
        }

        public void SetText(string sText)
        {
            if ( textObj.GetText() != sText )
                textObj.SetText(sText);
        }

        public override void SetColor(Colorf color)
        {
            textObj.SetColor(color);
        }
    }







    public class fTextAreaGameObject : fGameObject
    {
        Vector2f size;
        fText textObj;

        public fTextAreaGameObject(GameObject go, fText textObj, Vector2f size)
            : base(go, FGOFlags.EnablePreRender)
        {
            this.size = size;
            this.textObj = textObj;
        }

        public fText TextObject
        {
            get { return textObj; }
        }

        public Vector2f GetSize() {
            return size;
        }

        //public void SetHeight(float fNewHeight)
        //{
        //    // doesn't support textmeshpro text...

        //    TextMesh tm = go.GetComponent<TextMesh>();
        //    tm.transform.localScale = Vector3f.One;
        //    Vector2f size = UnityUtil.EstimateTextMeshDimensions(tm);
        //    float fScaleH = fNewHeight / size.y;
        //    tm.transform.localScale = new Vector3(fScaleH, fScaleH, fScaleH);
        //    size = new Vector2f(fScaleH * size.x, fNewHeight);
        //}

        public string GetText()
        {
            return textObj.GetText();
        }
        public void SetText(string sText)
        {
            if ( textObj.GetText() != sText )
                textObj.SetText(sText);
        }

        public override void SetColor(Colorf color)
        {
            textObj.SetColor(color);
        }
    }






    public class fCurveGameObject : fGameObject
    {
        float width = 0.05f;
        Colorf color = Colorf.Black;

        CurveRendererImplementation renderer;

        public fCurveGameObject(GameObject go, CurveRendererImplementation curveRenderer) 
            : base(go, FGOFlags.EnablePreRender)
        {
            renderer = curveRenderer;
        }


        public void SetLineWidth(float fWidth) {
            update(fWidth, color);
        }
        public float GetLineWidth() { return width; }

        public override void SetColor(Colorf newColor) {
            update(width, newColor);
        }
        public Colorf GetColor() { return color; }


        protected void update(float newWidth, Colorf newColor)
        {
            if (width != newWidth) {
                width = newWidth;
                renderer.update_width(width);
            }
            if (color != newColor) {
                color = newColor;
                renderer.update_color(color);
                base.SetColor(color);       // material overrides line renderer??
            }
        }

        protected void update_curve(Vector3f[] Vertices)
        {
            renderer.update_curve(Vertices);
        }

        protected void update_num_points(int N)
        {
            renderer.update_num_points(N);
        }
        
        protected void update_position(int i, Vector3f v)
        {
            renderer.update_position(i, v);
        }

    }





    public class fLineGameObject : fCurveGameObject
    {
        Vector3f start, end;

        public fLineGameObject(GameObject go, CurveRendererImplementation curveRenderer) 
            : base(go, curveRenderer)
        {
            LineRenderer r = go.GetComponent<LineRenderer>();
            r.positionCount = 2;
        }


        public void SetStart(Vector3f s) {
            if ( start != s ) {
                start = s;
                LineRenderer r = go.GetComponent<LineRenderer>();
                r.SetPosition(0, start);
            }
        }
        public Vector3f GetStart() { return start; }


        public void SetEnd(Vector3f e) {
            if ( end != e ) {
                end = e;
                LineRenderer r = go.GetComponent<LineRenderer>();
                r.SetPosition(1, end);
            }
        }
        public Vector3f GetEnd() { return end; }
    }





    public class fPolylineGameObject : fCurveGameObject
    {
        Vector3f[] Vertices;
        bool bVertsValid;

        public fPolylineGameObject(GameObject go, CurveRendererImplementation curveRenderer) 
            : base(go, curveRenderer)
        {
        }


        public void SetVertices(List<Vector3f> vertices) {
            Vertices = vertices.ToArray();
            bVertsValid = false;
        }


        public override void PreRender()
        {
            if (bVertsValid)
                return;
            update_curve(Vertices);
            bVertsValid = true;
        }
    }





    public class fCircleGameObject : fCurveGameObject
    {
        float radius = 1.0f;
        int steps = 32;
        bool bCircleValid = false;

        public fCircleGameObject(GameObject go, CurveRendererImplementation curveRenderer) 
            : base(go, curveRenderer)
        {
        }


        public void SetRadius(float fRadius) {
            if ( radius != fRadius ) {
                radius = fRadius;
                bCircleValid = false;
            }
        }
        public float GetRadius() { return radius; }


        public void SetSteps(int nSteps) {
            if ( steps != nSteps) {
                steps = nSteps;
                bCircleValid = false;
            }
        }
        public int GetSteps() { return steps; }

        public override void PreRender()
        {
            if (bCircleValid)
                return;

            update_num_points(steps + 1);
            float twopi = (float)(2 * Math.PI);
            for (int i = 0; i <= steps; ++i) {
                float t = (float)i / (float)steps;
                float a = t * twopi;
                float x = radius * (float)Math.Cos(a);
                float y = radius * (float)Math.Sin(a);
                update_position(i, new Vector3f(x, 0, y));
            }

            bCircleValid = true;
        }
    }





    public class fRectangleGameObject : fGameObject
    {
        float width = 1;
        float height = 1;

        public fRectangleGameObject(GameObject go, float widthIn = 1, float heightIn = 1)
            : base(go, FGOFlags.NoFlags)
        {
            width = widthIn;
            height = heightIn;
            SetLocalScale(new Vector3f(width, 1, height));
        }

        public void SetWidth(float fWidth) {
            if ( width != fWidth ) {
                width = fWidth;
                SetLocalScale(new Vector3f(width, 1, height));
            }
        }
        public float GetWidth() { return width; }

        public void SetHeight(float fHeight) {
            if ( height != fHeight ) {
                height = fHeight;
                SetLocalScale(new Vector3f(width, 1, height));
            }
        }
        public float GetHeight() { return height; }
    }




    public class fTriangleGameObject : fGameObject
    {
        float width = 1;
        float height = 1;

        public fTriangleGameObject(GameObject go, float widthIn = 1, float heightIn = 1)
            : base(go, FGOFlags.NoFlags)
        {
            width = widthIn;
            height = heightIn;
            SetLocalScale(new Vector3f(width, 1, height));
        }

        public void SetWidth(float fWidth) {
            if ( width != fWidth ) {
                width = fWidth;
                SetLocalScale(new Vector3f(width, 1, height));
            }
        }
        public float GetWidth() { return width; }

        public void SetHeight(float fHeight) {
            if ( height != fHeight ) {
                height = fHeight;
                SetLocalScale(new Vector3f(width, 1, height));
            }
        }
        public float GetHeight() { return height; }
    }





    public class fMeshGameObject : fGameObject
    {
        public fMesh Mesh;


        public fMeshGameObject() : base()
        {
        }

        public fMeshGameObject(fMesh mesh, bool bCreate = true, bool bAddCollider = false) : base()
        {
            Mesh = mesh;
            if ( bCreate ) {
                GameObject go = new GameObject();
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                if (bAddCollider)
                    go.AddComponent<MeshCollider>();
                Initialize(go, FGOFlags.NoFlags);
                UpdateMesh(Mesh, true, bAddCollider);
            }
        }


        public fMeshGameObject(GameObject go, fMesh mesh, FGOFlags flags)
            : base (go, flags)
        {
            Mesh = mesh;
        }

        public virtual void Initialize(GameObject go, fMesh mesh, FGOFlags flags)
        {
            base.Initialize(go, flags);
            Mesh = mesh;
        }

        public bool EnableCollisions
        {
            set {
                MeshCollider c = go.GetComponent<MeshCollider>();
                if (c != null)
                    c.enabled = value;
            }
        }


        public void UpdateMesh(fMesh m, bool bShared, bool bUpdateCollider)
        {
            if (bShared) {
                Mesh = m;
                go.SetSharedMesh(m, bUpdateCollider);
            } else {
                go.SetMesh(m, bUpdateCollider);
                Mesh = new fMesh(go.GetSharedMesh());
            }
        }
    }






    public class fDiscGameObject : fMeshGameObject
    {
        float radius = 1;
        float startAngleDeg = 0;
        float endAngleDeg = 360;
        bool bDiscValid;

        public fDiscGameObject() : base()
        {
            bDiscValid = false;
        }
        public fDiscGameObject(GameObject go, fMesh mesh, float radiusIn = 1)
            : base(go, mesh, FGOFlags.EnablePreRender)
        {
            radius = radiusIn;
            bDiscValid = false;
            SetLocalScale(new Vector3f(radius, 1, radius));
        }


        public virtual void Initialize(GameObject go, fMesh mesh, float radiusIn = 1)
        {
            base.Initialize(go, mesh, FGOFlags.EnablePreRender);
            radius = radiusIn;
            bDiscValid = false;
            SetLocalScale(new Vector3f(radius, 1, radius));
        }


        public void SetRadius(float fRadius) {
            if ( radius != fRadius ) {
                radius = fRadius;
                SetLocalScale(new Vector3f(radius, 1, radius));
            }
        }
        public float GetRadius() { return radius; }


        public void SetStartAngleDeg(float fAngle) {
            if ( startAngleDeg != fAngle ) {
                startAngleDeg = fAngle;
                bDiscValid = false;
            }
        }
        public float GetStartAngleDeg() { return startAngleDeg; }

        public void SetEndAngleDeg(float fAngle) {
            if ( endAngleDeg != fAngle ) {
                endAngleDeg = fAngle;
                bDiscValid = false;
            }
        }
        public float GetEndAngleDeg() { return endAngleDeg; }


        public override void PreRender()
        {
            if (bDiscValid)
                return;

            TrivialDiscGenerator discGen = new TrivialDiscGenerator() {
                StartAngleDeg = startAngleDeg, EndAngleDeg = endAngleDeg, Clockwise = false
            };
            discGen.Generate();
            fMesh newMesh = new fMesh(discGen.MakeUnityMesh());
            UpdateMesh(newMesh, true, true);

            bDiscValid = true;
        }

    }







    public class fRingGameObject : fMeshGameObject
    {
        float outerRadius = 1;
        float innerRadius = 1;
        float startAngleDeg = 0;
        float endAngleDeg = 360;
        bool bDiscValid;

        public fRingGameObject() : base()
        {
            bDiscValid = false;
        }
        public fRingGameObject(GameObject go, fMesh mesh, float outerRad = 1, float innerRad = 0.5f)
            : base(go, mesh, FGOFlags.EnablePreRender)
        {
            outerRadius = outerRad;
            innerRadius = innerRad;
            bDiscValid = false;
            SetLocalScale(Vector3f.One);
        }


        public virtual void Initialize(GameObject go, fMesh mesh, float outerRad = 1, float innerRad = 0.5f)
        {
            base.Initialize(go, mesh, FGOFlags.EnablePreRender);
            outerRadius = outerRad;
            innerRadius = innerRad;
            bDiscValid = false;
            SetLocalScale(Vector3f.One);
        }


        public void SetOuterRadius(float fRadius) {
            if ( outerRadius != fRadius ) {
                outerRadius = fRadius;
                bDiscValid = false;
            }
        }
        public float GetOuterRadius() { return outerRadius; }

        public void SetInnerRadius(float fRadius) {
            if ( innerRadius != fRadius ) {
                innerRadius = fRadius;
                bDiscValid = false;
            }
        }
        public float GetInnerRadius() { return innerRadius; }

        public void SetStartAngleDeg(float fAngle) {
            if ( startAngleDeg != fAngle ) {
                startAngleDeg = fAngle;
                bDiscValid = false;
            }
        }
        public float GetStartAngleDeg() { return startAngleDeg; }

        public void SetEndAngleDeg(float fAngle) {
            if ( endAngleDeg != fAngle ) {
                endAngleDeg = fAngle;
                bDiscValid = false;
            }
        }
        public float GetEndAngleDeg() { return endAngleDeg; }


        public override void PreRender()
        {
            if (bDiscValid)
                return;

            PuncturedDiscGenerator discGen = new PuncturedDiscGenerator() {
                OuterRadius = outerRadius, InnerRadius = innerRadius,
                StartAngleDeg = startAngleDeg, EndAngleDeg = endAngleDeg, Clockwise = false
            };
            discGen.Generate();
            fMesh newMesh = new fMesh(discGen.MakeUnityMesh());
            UpdateMesh(newMesh, true, true);

            bDiscValid = true;
        }

    }






    public class fBoxGameObject : fMeshGameObject
    {
        float width = 1;
        float height = 1;
        float depth = 1;
        bool bBoxValid;

        public fBoxGameObject() : base()
        {
            bBoxValid = false;
        }
        public fBoxGameObject(GameObject go, fMesh mesh, float widthIn = 1, float heightIn = 1, float depthIn = 1)
            : base(go, mesh, FGOFlags.EnablePreRender)
        {
            width = widthIn;
            height = heightIn;
            depth = depthIn;
            bBoxValid = false;
            SetLocalScale(new Vector3f(width, height, depth));
        }


        public virtual void Initialize(GameObject go, fMesh mesh, float widthIn = 1, float heightIn = 1, float depthIn = 1)
        {
            base.Initialize(go, mesh, FGOFlags.EnablePreRender);
            width = widthIn;
            height = heightIn;
            depth = depthIn;
            bBoxValid = false;
            SetLocalScale(new Vector3f(width, height, depth));
        }


        public void SetWidth(float fWidth) {
            if ( width != fWidth ) {
                width = fWidth;
                SetLocalScale(new Vector3f(width, height, depth));
            }
        }
        public float GetWidth() { return width; }

        public void SetHeight(float fHeight) {
            if ( height != fHeight ) {
                height = fHeight;
                SetLocalScale(new Vector3f(width, height, depth));
            }
        }
        public float GetHeight() { return height; }

        public void SetDepth(float fDepth) {
            if ( depth != fDepth ) {
                depth = fDepth;
                SetLocalScale(new Vector3f(width, height, depth));
            }
        }
        public float GetDepth() { return depth; }


        public override void PreRender()
        {
            if (bBoxValid)
                return;

            fMesh newMesh = PrimitiveCache.GetPrimitiveMesh(fPrimitiveType.Box);
            UpdateMesh(newMesh, true, true);
            bBoxValid = true;
        }

    }









    public class PreRenderBehavior : MonoBehaviour
    {
        public fGameObject ParentFGO = null;
        void Update() {
            // [RMS] this can be null if we created a GO by copying an fGO using Unity functions (eg Object.Instantiate).
            // The GO wil be created, and the PreRenderBehavior script will be copied, however there
            // is no ParentFGO. We need a deep-copy of FGOs to fix this!
            //
            // Currently only happens when using UnityWrapperSO? so not a crisis.
            if ( ParentFGO != null )
                ParentFGO.PreRender();
        }
    }



}
