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


        protected float SliderWidth = 10;
        protected float SliderHeight = 1;
        protected Colorf bgColor = Colorf.White;


        protected Colorf handleColor = Colorf.Black;
        protected fMaterial handleMaterial;



        int tick_count = 0;
        public virtual int TickCount
        {
            get { return tick_count; }
            set { tick_count = MathUtil.Clamp(value, 0, 1000); update_geometry(); }
        }
        protected fMaterial TickMaterial;
        Colorf tickColor = Colorf.VideoBlack;

        public bool TicksAreVisible { get; set; }

        public enum TickSnapModes
        {
            NoSnapping,
            AlwaysSnapToNearest,
            SnapWithinThreshold
        }
        TickSnapModes snap_mode;
        public virtual TickSnapModes TickSnapMode
        {
            get { return snap_mode; }
            set { snap_mode = value; update_value(current_value, false); }
        }

        public float TickSnapThreshold = 0.05f;

        public virtual Vector2f TickDimensions {
            get { return new Vector2f(Height * 0.1f, Height * 1.0f); }
        }


        public enum LabelPositionType
        {
            CenteredBelow,
            CenteredAbove,
            BelowLeftAligned,
            BelowRightAligned
        }
        class PositionLabel {
            public string text;
            public Colorf color;
            public LabelPositionType position;
            public Vector2f offset;
            public fTextGameObject go;
        };
        Dictionary<double, PositionLabel> Labels = new Dictionary<double, PositionLabel>();

        public float LabelTextHeight { get; set; }
        public bool LabelsAreVisible { get; set; }




        public HUDSliderBase()
        {
            TicksAreVisible = true;
            LabelsAreVisible = true;
            LabelTextHeight = SliderHeight * 0.5f;
        }


        public virtual void Create()
        {
            rootGO = new GameObject(UniqueNames.GetNext("HUDSlider"));

            fMaterial useMaterial = MaterialUtil.CreateFlatMaterialF(bgColor);
            backgroundGO = GameObjectFactory.CreateRectangleGO("background",
                SliderWidth, SliderHeight, useMaterial, true, true);
            MaterialUtil.DisableShadows(backgroundGO);
            backgroundGO.RotateD(Vector3f.AxisX, -90.0f); // ??
            AppendNewGO(backgroundGO, rootGO, false);

            handleMaterial = MaterialUtil.CreateFlatMaterialF(handleColor);
            handleGO = create_handle_go(handleMaterial);
            if (handleGO != null) {
                handleGO.Translate(0.001f * Vector3f.AxisY, true);
                AppendNewGO(handleGO, rootGO, false);
                handleStart = handleGO.GetLocalFrame();
            }

            create_visuals_geometry();

            TickMaterial = MaterialUtil.CreateFlatMaterialF(tickColor);

            update_geometry();
        }





        

        /// <summary>
        /// Create the GO that acts as the "handle", ie identifies selected position on timelin
        /// Default is a kind of ugly large triangle underneath midline...
        /// Override to provide your own visuals. Return null for no handle.
        /// </summary>
        protected virtual fGameObject create_handle_go(fMaterial handleMaterial)
        {
            fTriangleGameObject handle = GameObjectFactory.CreateTriangleGO(
                "handle", Height, Height, handleMaterial.color, true);
            MaterialUtil.DisableShadows(handle);
            handle.RotateD(Vector3f.AxisX, -90.0f); // ??
            return handle;
        }

        /// <summary>
        /// Update handle sizing. Width and Height here are full slider width/height
        /// </summary>
        protected virtual void update_handle_go(fGameObject handleGO, float width, float height)
        {
            if ( handleGO is fTriangleGameObject == false )
                DebugUtil.Error("HUDSliderBase: handle is not standard type but get_handle_offset not overloaded!");

            (handleGO as fTriangleGameObject).SetHeight(height * 0.5f);
            (handleGO as fTriangleGameObject).SetWidth(height * 0.5f);
        }


        /// <summary>
        /// Get up/down shift of handle relative to midline of slider.
        /// </summary>
        protected virtual float get_handle_offset(fGameObject handleGO)
        {
            if ( handleGO is fTriangleGameObject == false )
                DebugUtil.Error("HUDSliderBase: handle is not standard type but get_handle_offset not overloaded!");

            float h = (handleGO as fTriangleGameObject).GetHeight();
            float dz = -(SliderHeight / 2) - h / 2;
            return dz;
        }



        // override this to add extra UI stuff 
        // [TODO] remove this? what for? 
        protected virtual void create_visuals_geometry()
        {
        }



        public event InputEventHandler OnClicked;

        public event BeginValueChangeHandler OnValueChangeBegin;
        public event ValueChangedHandler OnValueChanged;
        public event EndValueChangeHandler OnValueChangeEnd;


        double current_value;
        double snapped_value;



        public virtual float Width {
            get { return SliderWidth; }
            set {
                if (SliderWidth != value) {
                    SliderWidth = value;
                    update_geometry();
                }
            }
        }
        public virtual float Height {
            get { return SliderHeight; }
            set {
                if (SliderHeight != value) {
                    SliderHeight = value;
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


        public virtual Colorf TickColor
        {
            get { return tickColor; }
            set {
                tickColor = value;
                // todo: handle dynamic updates
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



        public virtual void AddLabel(double atValue, string text, Colorf color, LabelPositionType position, Vector2f offset )
        {
            PositionLabel label = new PositionLabel();
            label.text = text;
            label.color = color;
            label.position = position;
            label.offset = offset;

            if ( Labels.ContainsKey(atValue) ) {
                RemoveGO((fGameObject)Labels[atValue].go);
                Labels[atValue].go.Destroy();
            }

            Labels[atValue] = label;
        }

        public virtual void ClearLabels()
        {
            foreach ( var labelinfo in Labels.Values ) {
                if (labelinfo.go != null) {
                    RemoveGO((fGameObject)labelinfo.go);
                    labelinfo.go.Destroy();
                }
            }
            Labels.Clear();
        }



        protected virtual void update_geometry()
        {
            if (rootGO == null)
                return;

            backgroundGO.SetWidth(SliderWidth);
            backgroundGO.SetHeight(SliderHeight);

            if (handleGO != null)
                update_handle_go(handleGO, SliderWidth, SliderHeight);

            update_handle_position();
            update_ticks();
            update_labels();
        }


        protected virtual void update_handle_position()
        {
            if (handleGO == null)
                return;

            float t = (float)(snapped_value - 0.5);
            Frame3f handleF = handleStart;
            handleF.Translate(SliderWidth * t * handleF.X);
            float dz = get_handle_offset(handleGO);
            handleF.Translate(dz * handleF.Z);
            handleGO.SetLocalFrame(handleF);
        }




        protected void update_labels()
        {
            AxisAlignedBox2f localBounds = BoxModel.LocalBounds(this);

            foreach ( var pair in Labels ) {
                float t = (float)(pair.Key);
                PositionLabel labelinfo = pair.Value;

                if ( labelinfo.go == null ) {
                    BoxPosition boxPos = BoxPosition.CenterTop;
                    if (labelinfo.position == LabelPositionType.CenteredAbove)
                        boxPos = BoxPosition.CenterBottom;
                    else if (labelinfo.position == LabelPositionType.BelowLeftAligned)
                        boxPos = BoxPosition.TopLeft;
                    else if (labelinfo.position == LabelPositionType.BelowRightAligned)
                        boxPos = BoxPosition.TopRight;

                    labelinfo.go = GameObjectFactory.CreateTextMeshGO("sliderlabel_"+labelinfo.text, labelinfo.text,
                        labelinfo.color, LabelTextHeight, boxPos);
                    AppendNewGO(labelinfo.go, rootGO, false);
                }

                if (labelinfo.position == LabelPositionType.CenteredBelow ||
                    labelinfo.position == LabelPositionType.BelowLeftAligned ||
                    labelinfo.position == LabelPositionType.BelowRightAligned) {
                    BoxModel.MoveTo(labelinfo.go, localBounds.BottomLeft, -Height * 0.01f);
                } else if (labelinfo.position == LabelPositionType.CenteredAbove) {
                    BoxModel.MoveTo(labelinfo.go, localBounds.TopLeft, -Height * 0.01f);
                } else
                    throw new NotSupportedException("HUDSliderBase.update_labels : unhandled LabelPositionType");

                BoxModel.Translate(labelinfo.go, new Vector2f(t * Width, 0) + labelinfo.offset );
            }
        }




        /// <summary>
        /// Create GO / geometry for a single tick, centered at origin
        /// </summary>
        protected virtual fGameObject create_tick_go(Vector2f tickSize, fMaterial baseMaterial)
        {
            fRectangleGameObject go = GameObjectFactory.CreateRectangleGO("tick", tickSize.x, tickSize.y,
                baseMaterial, true, false);
            MaterialUtil.DisableShadows(go);
            go.RotateD(Vector3f.AxisX, -90.0f);
            return go;
        }

        /// <summary>
        /// Update GO for a single tick created by CreateTickGO(). 
        /// This does not require positioning the tick, that happens automatically.
        /// fT is in range [0,1], can be used for styling/etc
        /// </summary>
        protected virtual void update_tick_go(int iTick, fGameObject go, Vector2f tickSize, float fT )
        {
            fRectangleGameObject rectGO = go as fRectangleGameObject;
            rectGO.SetWidth(tickSize.x);
            rectGO.SetHeight(tickSize.y);
        }




        struct TickGO {
            public fGameObject go;
        }
        List<TickGO> ticks = new List<TickGO>();


        int tick_count_cache = -1;
        protected virtual void update_ticks()
        {
            if ( TicksAreVisible == false ) {
                foreach (var tick in ticks)
                    tick.go.SetVisible(false);
                return;
            }


            TickMaterial.color = tickColor;
            Vector2f tickSize = TickDimensions;
            AxisAlignedBox2f localBounds = BoxModel.LocalBounds(this);

            // create extra ticks if we need them
            if ( tick_count > tick_count_cache ) {
                while (ticks.Count < tick_count) {
                    TickGO tick = new TickGO();
                    tick.go = create_tick_go(tickSize, TickMaterial);
                    AppendNewGO(tick.go, rootGO, false);
                    BoxModel.Translate(tick.go, Vector2f.Zero, localBounds.CenterLeft, -Height*0.01f);
                    ticks.Add(tick);
                }
                tick_count_cache = tick_count;
            }

            // align and show/hide ticks
            for ( int i = 0; i < ticks.Count; ++i ) {
                fGameObject go = ticks[i].go;
                if (i < tick_count) {
                    float t = (float)i / (float)(tick_count-1);
                    update_tick_go(i, go, tickSize, t);
                    BoxModel.MoveTo(go, localBounds.CenterLeft, -Height * 0.01f);
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

            if (TickSnapMode != TickSnapModes.NoSnapping && TickCount > 0) {
                if (TickSnapMode == TickSnapModes.AlwaysSnapToNearest) {
                    double fTickSpan = 1.0 / (TickCount-1);
                    double fSnapped = Snapping.SnapToIncrement(snapped_value, fTickSpan);
                    fSnapped = MathUtil.Clamp(fSnapped, 0, 1);
                    snapped_value = fSnapped;
                } else {
                    double fTickSpan = 1.0 / (TickCount-1);
                    double fSnapped = Snapping.SnapToIncrement(snapped_value, fTickSpan);
                    if (Math.Abs(fSnapped - snapped_value) < TickSnapThreshold)
                        snapped_value = fSnapped;
                }
            }

            update_handle_position();

            if ( bSendEvent )
                FUtil.SafeSendEvent(OnValueChanged, this, prev, snapped_value);
        }


        protected virtual double get_slider_tx(Vector3f posW)
        {
            // assume slider is centered at origin of root node
            Vector3f posL = rootGO.PointToLocal(posW);
            float tx = (posL.x - (-SliderWidth/2) ) / SliderWidth;
            tx = MathUtil.Clamp(tx, 0, 1);
            return tx;            
        }


        protected virtual void onHandlePress(InputEvent e, Vector3f hitPosW)
        {
            FUtil.SafeSendEvent(OnValueChangeBegin, this, snapped_value);
        }

        protected virtual void onHandlePressDrag(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            update_value(t, true);
        }

        protected virtual void onSliderbarPress(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            FUtil.SafeSendEvent(OnValueChangeBegin, this, snapped_value);
            update_value(t, true);
        }

        protected virtual void onSliderBarPressDrag(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            update_value(t, true);
        }



        protected virtual void onSliderbarClick(InputEvent e, Vector3f hitPosW)
        {
            FUtil.SafeSendEvent(OnClicked, this, e);
        }




		#region SceneUIElement implementation

		override public fGameObject RootGameObject {
			get { return rootGO; }
		}


        protected enum InteractionMode
        {
            InHandleDrag, InPressDrag, InCustom, None
        }
        InteractionMode eInterMode;

        override public bool WantsCapture(InputEvent e)
        {
            return (Enabled && HasGO(e.hit.hitGO));
        }

        Vector3f vStartHitW;
        Frame3f vHandleStartW;


        // subclass can implement these to add custom behaviors
        virtual public bool custom_begin_capture(InputEvent e, GameObjectRayHit hit)
        {
            return false;
        }
        virtual public void custom_update_capture(InputEvent e)
        {
        }
        virtual public void custom_end_capture(InputEvent e)
        {
        }


        override public bool BeginCapture (InputEvent e)
		{
            eInterMode = InteractionMode.None;

            GameObjectRayHit hit;
            if (! FindGORayIntersection(e.ray, out hit) )
                return false;       // this should not be possible...
            if ( custom_begin_capture(e, hit) ) {
                eInterMode = InteractionMode.InCustom;
            } else {
                standard_begin_capture(e, hit);
            }

            return (eInterMode != InteractionMode.None);
		}
        protected void standard_begin_capture(InputEvent e, GameObjectRayHit hit)
        {
            if (handleGO.IsSameOrChild(hit.hitGO)) {
                onHandlePress(e, hit.hitPos);
                eInterMode = InteractionMode.InHandleDrag;
                vStartHitW = hit.hitPos;
                vHandleStartW = handleGO.GetWorldFrame();
            } else if (backgroundGO.IsSameOrChild(hit.hitGO)) {
                onSliderbarPress(e, hit.hitPos);
                eInterMode = InteractionMode.InPressDrag;
                vStartHitW = hit.hitPos;
                vHandleStartW = handleGO.GetWorldFrame();
            }
        }

		override public bool UpdateCapture (InputEvent e)
		{
            if ( eInterMode == InteractionMode.InCustom ) {
                custom_update_capture(e);
            } else {
                standard_update_capture(e, eInterMode);
            }

            return true;
		}
        protected void standard_update_capture(InputEvent e, InteractionMode eMode)
        {
            if (eMode == InteractionMode.InPressDrag) {
                Vector3f hitPos = vHandleStartW.RayPlaneIntersection(e.ray.Origin, e.ray.Direction, 1);
                onSliderBarPressDrag(e, hitPos);

            } else if (eMode == InteractionMode.InHandleDrag) {
                Vector3f hitPos = vHandleStartW.RayPlaneIntersection(e.ray.Origin, e.ray.Direction, 1);
                Vector3f dv = hitPos - vStartHitW;
                Vector3f vRelPos = vHandleStartW.Origin + dv;
                onHandlePressDrag(e, vRelPos);
            }
        }

		override public bool EndCapture (InputEvent e)
		{
            if ( eInterMode == InteractionMode.InCustom ) {
                custom_end_capture(e);

            } else {
                standard_end_capture(e, eInterMode);
            }
			return true;
		}
        protected void standard_end_capture(InputEvent e, InteractionMode eMode)
        {
            if (eMode == InteractionMode.InHandleDrag || eMode == InteractionMode.InPressDrag) {
                FUtil.SafeSendEvent(OnValueChangeEnd, this, snapped_value);
            }
        }


        protected InteractionMode ActiveSliderInteraction {
            get { return eInterMode; }
        }

		#endregion





       #region IBoxModelElement implementation


        public Vector2f Size2D {
            get { return new Vector2f(Width, Height); }
        }

        public AxisAlignedBox2f Bounds2D { 
            get {
                Vector2f origin2 = RootGameObject.GetLocalPosition().xy;
                return new AxisAlignedBox2f(origin2, Width/2, Height/2);
            }
        }

        #endregion


    }
}
