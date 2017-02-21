using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public class HUDSliderBase : HUDStandardItem, IBoxModelElement
    {
        fGameObject rootGO;

        // should go in subclass...?
        fRectangleGameObject backgroundGO;

        fGameObject handleGO;
        Frame3f handleStart;


        int tick_count = 0;
        public virtual int TickCount
        {
            get { return tick_count; }
            set { tick_count = MathUtil.Clamp(value, 0, 1000); update_geometry(); }
        }
        fMaterial tickMaterial;
        Colorf tickColor = Colorf.VideoBlack;

        bool snap_to_ticks = false;
        public virtual bool EnableSnapToTicks
        {
            get { return snap_to_ticks; }
            set { snap_to_ticks = value; update_value(current_value, false); }
        }

        public virtual Vector2f TickDimensions {
            get { return new Vector2f(Height * 0.1f, Height * 1.0f); }
        }


        public virtual void Create()
        {
            rootGO = new GameObject(UniqueNames.GetNext("HUDSlider"));

            fMaterial useMaterial = MaterialUtil.CreateFlatMaterialF(bgColor);
            backgroundGO = GameObjectFactory.CreateRectangleGO("background",
                width, height, useMaterial, true, true);
            MaterialUtil.DisableShadows(backgroundGO);
            backgroundGO.RotateD(Vector3f.AxisX, -90.0f); // ??
            AppendNewGO(backgroundGO, rootGO, false);

            handleGO = create_handle_go();
            if (handleGO != null) {
                handleGO.Translate(0.001f * Vector3f.AxisY);
                AppendNewGO(handleGO, rootGO, false);
                handleStart = handleGO.GetLocalFrame();
            }

            create_visuals_geometry();

            tickMaterial = MaterialUtil.CreateFlatMaterialF(tickColor);

            update_geometry();
        }



        protected float width = 10;
        protected float height = 1;
        protected Colorf bgColor = Colorf.White;
        protected Colorf handleColor = Colorf.Black;


        

        // override this to change handle shape
        public virtual fGameObject create_handle_go()
        {
            fTriangleGameObject handle = GameObjectFactory.CreateTriangleGO(
                "handle", Height, Height, handleColor, true);
            MaterialUtil.DisableShadows(handle);
            handle.RotateD(Vector3f.AxisX, -90.0f); // ??
            return handle;
        }


        // override this to add extra UI stuff that 
        public virtual void create_visuals_geometry()
        {
        }



        public event InputEventHandler OnClicked;

        public event BeginValueChangeHandler OnValueChangeBegin;
        public event ValueChangedHandler OnValueChanged;
        public event EndValueChangeHandler OnValueChangeEnd;


        double current_value;
        double snapped_value;



        public virtual float Width {
            get { return width; }
            set {
                if (width != value) {
                    width = value;
                    update_geometry();
                }
            }
        }
        public virtual float Height {
            get { return height; }
            set {
                if (height != value) {
                    height = value;
                    update_geometry();
                }
            }
        }

        public virtual Colorf BackgroundColor
        {
            get { return bgColor; }
            set {
                bgColor = value;
                if ( backgroundGO != null )
                    backgroundGO.SetColor(bgColor);
            }
        }


        public virtual Colorf HandleColor
        {
            get { return handleColor; }
            set {
                handleColor = value;
                if ( handleGO != null )
                    handleGO.SetColor(handleColor);
            }
        }


        public virtual double Value
        {
            get { return snapped_value; }
            set {
                if (current_value != value)
                    update_value(value, true);
            }
        }


        protected virtual void update_geometry()
        {
            if (rootGO == null)
                return;

            backgroundGO.SetWidth(width);
            backgroundGO.SetHeight(height);

            if (handleGO != null) {
                (handleGO as fTriangleGameObject).SetHeight(height * 0.5f);
                (handleGO as fTriangleGameObject).SetWidth(height * 0.5f);
            }

            update_handle_position();

            update_ticks();
        }


        void update_handle_position()
        {
            if (handleGO == null)
                return;

            float t = (float)(snapped_value - 0.5);
            Frame3f handleF = handleStart;
            handleF.Translate(width * t * handleF.X);
            float h = (handleGO as fTriangleGameObject).GetHeight();
            float dz = -(height/2) - h/2;
            handleF.Translate(dz * handleF.Z);
            handleGO.SetLocalFrame(handleF);
        }





        struct TickGO {
            public fRectangleGameObject go;
        }
        List<TickGO> ticks = new List<TickGO>();


        int tick_count_cache = -1;
        void update_ticks()
        {
            tickMaterial.color = tickColor;

            Vector2f tickSize = TickDimensions;

            // create extra ticks if we need them
            if ( tick_count > tick_count_cache ) {
                while (ticks.Count < tick_count) {
                    TickGO tick = new TickGO();
                    tick.go = GameObjectFactory.CreateRectangleGO("tick", tickSize.x, tickSize.y, 
                        tickMaterial, true, false);
                    tick.go.RotateD(Vector3f.AxisX, -90.0f);
                    AppendNewGO(tick.go, rootGO, false);
                    BoxModel.Translate(tick.go, Vector2f.Zero, this.Bounds2D.CenterLeft, -Height*0.01f);
                    ticks.Add(tick);
                }
                tick_count_cache = tick_count;
            }

            // align and show/hide ticks
            for ( int i = 0; i < ticks.Count; ++i ) {
                fRectangleGameObject go = ticks[i].go;
                if (i < tick_count) {
                    float t = (float)i / (float)(tick_count-1);
                    ticks[i].go.SetWidth(tickSize.x);
                    ticks[i].go.SetHeight(tickSize.y);
                    BoxModel.MoveTo(go, this.Bounds2D.CenterLeft, -Height * 0.01f);
                    BoxModel.Translate(go, new Vector2f(t * Width, 0));
                    go.SetVisible(true);
                } else {
                    ticks[i].go.SetVisible(false);
                }
            }

        }






        protected virtual void update_value(double newValue, bool bSendEvent)
        {
            double prev = current_value;
            current_value = newValue;

            snapped_value = current_value;

            if (EnableSnapToTicks && TickCount > 0) {
                double fTickSpan = 1.0 / (TickCount-1);
                double fSnapped = Snapping.SnapToIncrement(snapped_value, fTickSpan);
                fSnapped = MathUtil.Clamp(fSnapped, 0, 1);
                // [RMS] only snap when close enough to tick?
                //double fSnapT = fTickSpan * 0.25;
                //if (Math.Abs(fSnapped - snapped_value) < fSnapT)
                    snapped_value = fSnapped;
            }

            update_handle_position();

            if ( bSendEvent )
                FUtil.SafeSendEvent(OnValueChanged, this, prev, snapped_value);
        }


        double get_slider_tx(Vector3f posW)
        {
            // assume slider is centered at origin of root node
            Vector3f posL = rootGO.PointToLocal(posW);
            float tx = (posL.x - (-width/2) ) / width;
            tx = MathUtil.Clamp(tx, 0, 1);
            return tx;            
        }


        void onHandlePress(InputEvent e, Vector3f hitPosW)
        {
            FUtil.SafeSendEvent(OnValueChangeBegin, this, snapped_value);
        }

        void onHandlePressDrag(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            update_value(t, true);
        }

        void onSliderbarPress(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            FUtil.SafeSendEvent(OnValueChangeBegin, this, snapped_value);
            update_value(t, true);
        }

        void onSliderBarPressDrag(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            update_value(t, true);
        }



        void onSliderbarClick(InputEvent e, Vector3f hitPosW)
        {
            FUtil.SafeSendEvent(OnClicked, this, e);
        }




		#region SceneUIElement implementation

		override public GameObject RootGameObject {
			get { return rootGO; }
		}


        enum InteractionMode
        {
            InHandleDrag, InPressDrag
        }
        InteractionMode eInterMode;

        override public bool WantsCapture(InputEvent e)
        {
            return (Enabled && HasGO(e.hit.hitGO));
        }

        Vector3f vStartHitW;
        Frame3f vHandleStartW;

        override public bool BeginCapture (InputEvent e)
		{
            GameObjectRayHit hit;
            if (! FindGORayIntersection(e.ray, out hit) )
                return false;       // this should not be possible...
            if ( hit.hitGO == handleGO ) {
                onHandlePress(e, hit.hitPos);
                eInterMode = InteractionMode.InHandleDrag;
                vStartHitW = hit.hitPos;
                vHandleStartW = handleGO.GetWorldFrame();
            } else if ( hit.hitGO == backgroundGO ) {
                onSliderbarPress(e, hit.hitPos);
                eInterMode = InteractionMode.InPressDrag;
                vStartHitW = hit.hitPos;
                vHandleStartW = handleGO.GetWorldFrame();
            }
            return true;
		}

		override public bool UpdateCapture (InputEvent e)
		{
            if ( eInterMode == InteractionMode.InPressDrag ) {
                Vector3f hitPos = vHandleStartW.RayPlaneIntersection(e.ray.Origin, e.ray.Direction, 1);
                onSliderBarPressDrag(e, hitPos);

            } else if ( eInterMode == InteractionMode.InHandleDrag ) {
                Vector3f hitPos = vHandleStartW.RayPlaneIntersection(e.ray.Origin, e.ray.Direction, 1);
                Vector3f dv = hitPos - vStartHitW;
                Vector3f vRelPos = vHandleStartW.Origin + dv;
                onHandlePressDrag(e, vRelPos);
            }

            return true;
		}

		override public bool EndCapture (InputEvent e)
		{
            if ( eInterMode == InteractionMode.InHandleDrag || eInterMode == InteractionMode.InPressDrag ) {
                FUtil.SafeSendEvent(OnValueChangeEnd, this, snapped_value);
            }
			return true;
		}

		#endregion





       #region IBoxModelElement implementation


        public Vector2f Size2D {
            get { return new Vector2f(Width, Height); }
        }

        public AxisAlignedBox2f Bounds2D { 
            get { return new AxisAlignedBox2f(Vector2f.Zero, Width/2, Height/2); }
        }

        #endregion


    }
}
