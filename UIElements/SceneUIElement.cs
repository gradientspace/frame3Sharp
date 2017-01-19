using System;
using UnityEngine;
using g3;

namespace f3
{
    public class InputEvent
    {
        public Ray3f ray;
        public CaptureSide side;
        public InputDevice device;
        public InputState input;

        public AnyRayHit hit;      // only set for WantsCapture / BeginCapture calls...


        public static InputEvent Mouse(InputState input, AnyRayHit hit = null) {
            return new InputEvent() {
                ray = input.vMouseWorldRay, side = CaptureSide.Any, device = InputDevice.Mouse, input = input, hit = hit };
        }
        public static InputEvent Gamepad(InputState input, AnyRayHit hit = null) {
            return new InputEvent() {
                ray = input.vGamepadWorldRay, side = CaptureSide.Any, device = InputDevice.Gamepad, input = input, hit = hit };
        }
        public static InputEvent Touch(InputState input, AnyRayHit hit = null) {
            return new InputEvent() {
                ray = input.vTouchWorldRay, side = CaptureSide.Any, device = InputDevice.TabletFingers, input = input, hit = hit };
        }
        public static InputEvent OculusTouch(CaptureSide which, InputState input, AnyRayHit hit = null) {
            return new InputEvent() {
                ray = (which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay,
                side = which, device = InputDevice.OculusTouch, input = input, hit = hit };
        }

    }


    public delegate void InputEventHandler(object source, InputEvent e);

    public delegate void BeginValueChangeHandler(object source, double fStartValue);
    public delegate void EndValueChangeHandler(object source, double fEndValue);
    public delegate void ValueChangedHandler(object source, double fOldValue, double fNewValue);

    public delegate void TextChangedHander(object source, string newText);

    public delegate void EditStateChangeHandler(object source);


    public interface SceneUIElement
	{
		GameObject RootGameObject{ get; }

		void Disconnect();

        bool IsVisible { get; set; }
        void SetLayer(int nLayer);

        // called on per-frame Update()
        void PreRender();

        bool FindRayIntersection(Ray ray, out UIRayHit hit);
        bool FindHoverRayIntersection(Ray ray, out UIRayHit hit);

        // temporary?
        bool WantsCapture(InputEvent e);
        bool BeginCapture(InputEvent e);
        bool UpdateCapture(InputEvent e);
        bool EndCapture(InputEvent e);

        // we only get a hover event if we returned true from FindHoverRayIntersection
        bool EnableHover { get; }
        void UpdateHover(Ray ray, UIRayHit hit);
        void EndHover(Ray ray);
    }



}

