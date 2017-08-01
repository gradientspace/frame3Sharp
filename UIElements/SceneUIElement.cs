using System;
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
        public static InputEvent HTCVive(CaptureSide which, InputState input, AnyRayHit hit = null) {
            return new InputEvent() {
                ray = (which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay,
                side = which, device = InputDevice.HTCViveWands, input = input, hit = hit };
        }
        public static InputEvent Spatial(CaptureSide which, InputState input, AnyRayHit hit = null) {
            return new InputEvent() {
                ray = (which == CaptureSide.Left) ? input.vLeftSpatialWorldRay : input.vRightSpatialWorldRay,
                side = which, device = InputDevice.AnySpatialDevice, input = input, hit = hit };
        }

    }


    public delegate void InputEventHandler(object source, InputEvent e);

    public delegate void BeginValueChangeHandler(object source, double fStartValue);
    public delegate void EndValueChangeHandler(object source, double fEndValue);
    public delegate void ValueChangedHandler(object source, double fOldValue, double fNewValue);

    public delegate void TextChangedHander(object source, string newText);

    public delegate void EditStateChangeHandler(object source);


    // object that is 'owner' of scene UI elements. This is both
    // top-level things like Cockpit & Scene, and also intermediate
    // parents like HUDPanel, HUDCollections, etc
    public interface SceneUIParent
    {
        FContext Context { get; }
    }


    /// <summary>
    /// SceneUIElement is the base class for all UI objects - ie things like buttons, 3D gizmos, etc.
    /// These are different from SceneObjects in that they are not persistent, do not have change history, etc.
    /// </summary>
    public interface SceneUIElement
	{
		fGameObject RootGameObject{ get; }

        string Name { get; set; }
		SceneUIParent Parent { get; set; }

        /// <summary>
        /// Disconnect is called to un-hook this object in preparation for removing it from Cockpit/Scene.
        /// The event OnDisconnect() must be fired inside Disconnect() implementations.
        /// Ideally it should be possible to re-add the UIElement later, however this is not required.
        /// </summary>
		void Disconnect();
        event EventHandler OnDisconnected;

        bool IsVisible { get; set; }
        bool IsInteractive { get; set; }
        void SetLayer(int nLayer);

        // called on per-frame Update()
        void PreRender();

        bool FindRayIntersection(Ray3f ray, out UIRayHit hit);
        bool FindHoverRayIntersection(Ray3f ray, out UIRayHit hit);

        // temporary?
        bool WantsCapture(InputEvent e);
        bool BeginCapture(InputEvent e);
        bool UpdateCapture(InputEvent e);
        bool EndCapture(InputEvent e);

        // we only get a hover event if we returned true from FindHoverRayIntersection
        bool EnableHover { get; }
        void UpdateHover(Ray3f ray, UIRayHit hit);
        void EndHover(Ray3f ray);

    }



}

