using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{
    public class HUDSliderBase : HUDStandardItem
    {
        fGameObject rootGO;

        // should go in subclass...
        fGameObject sliderbarGO;
        fTriangleGameObject handleGO;

        Frame3f handleStart;


        public void Create()
        {
            rootGO = new GameObject(UniqueNames.GetNext("HUDSlider"));

            sliderbarGO = GameObjectFactory.CreateRectangleGO(rootGO.GetName() + "_bar",
                width, height, bgColor, true);
            MaterialUtil.DisableShadows(sliderbarGO);
            sliderbarGO.RotateD(Vector3f.AxisX, -90.0f); // ??
            AppendNewGO(sliderbarGO, rootGO, false);

            handleGO = GameObjectFactory.CreateTriangleGO(rootGO.GetName() + "_handle",
                height, height, handleColor, true);
            MaterialUtil.DisableShadows(handleGO);
            handleGO.RotateD(Vector3f.AxisX, -90.0f); // ??
            handleGO.Translate(0.001f * Vector3f.AxisY);
            AppendNewGO(handleGO, rootGO, false);

            handleStart = handleGO.GetLocalFrame();

            update_geometry();
        }


        public event InputEventHandler OnClicked;

        public event BeginValueChangeHandler OnValueChangeBegin;
        public event ValueChangedHandler OnValueChanged;
        public event EndValueChangeHandler OnValueChangeEnd;


        double current_value;



        float width = 10;
        float height = 1;
        Colorf bgColor = Colorf.White;
        Colorf handleColor = Colorf.Black;

        public float Width {
            get { return width; }
            set {
                if (width != value) {
                    width = value;
                    update_geometry();
                }
            }
        }
        public float Height {
            get { return height; }
            set {
                if (height != value) {
                    height = value;
                    update_geometry();
                }
            }
        }

        public Colorf BackgroundColor
        {
            get { return bgColor; }
            set {
                bgColor = value;
                sliderbarGO.SetColor(bgColor);
            }
        }


        public Colorf HandleColor
        {
            get { return handleColor; }
            set {
                handleColor = value;
                handleGO.SetColor(handleColor);
            }
        }


        public double Value
        {
            get { return current_value; }
            set {
                if (current_value != value)
                    update_value(value, true);
            }
        }


        void update_geometry()
        {
            if (rootGO == null)
                return;

            (sliderbarGO as fRectangleGameObject).SetWidth(width);
            (sliderbarGO as fRectangleGameObject).SetHeight(height);

            handleGO.SetHeight(height*0.5f);
            handleGO.SetWidth(height*0.5f);

            update_handle_position();
        }


        void update_handle_position()
        {
            float t = (float)(current_value - 0.5);
            Frame3f handleF = handleStart;
            handleF.Translate(width * t * handleF.X);
            float h = handleGO.GetHeight();
            float dz = -(height/2) - h/2;
            handleF.Translate(dz * handleF.Z);
            handleGO.SetLocalFrame(handleF);
        }



        void update_value(double newValue, bool bSendEvent)
        {
            double prev = current_value;
            current_value = newValue;
            update_handle_position();
            if ( bSendEvent )
                UnityUtil.SafeSendEvent(OnValueChanged, this, prev, current_value);
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
            UnityUtil.SafeSendEvent(OnValueChangeBegin, this, current_value);
        }

        void onHandlePressDrag(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            update_value(t, true);
        }

        void onSliderbarPress(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            UnityUtil.SafeSendEvent(OnValueChangeBegin, this, current_value);
            update_value(t, true);
        }

        void onSliderBarPressDrag(InputEvent e, Vector3f hitPosW)
        {
            double t = get_slider_tx(hitPosW);
            update_value(t, true);
        }



        void onSliderbarClick(InputEvent e, Vector3f hitPosW)
        {
            UnityUtil.SafeSendEvent(OnClicked, this, e);
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
            bool bHit = FindGORayIntersection(e.ray, out hit);
            if ( hit.hitGO == handleGO ) {
                onHandlePress(e, hit.hitPos);
                eInterMode = InteractionMode.InHandleDrag;
                vStartHitW = hit.hitPos;
                vHandleStartW = handleGO.GetWorldFrame();
            } else if ( hit.hitGO == sliderbarGO ) {
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
                UnityUtil.SafeSendEvent(OnValueChangeEnd, this, current_value);
            }
			return true;
		}

		#endregion


    }
}
