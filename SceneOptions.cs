using UnityEngine;
using System.Collections;

namespace f3
{
    // passed to Cockpit.Start()
    public interface ICockpitInitializer
    {
        void Initialize(Cockpit cockpit);
    }

    // called by SceneController.Start()
    public interface ISceneInitializer
    {
        void Initialize(FScene scene);
    }

    // configuration for f3 framework
    public class SceneOptions
    {
        public int LogLevel { get; set; }


        // If true, SceneLightingSetup object is attached to Scene, which creates basic scene lighting
        // If false, you are responsible for your own lighting
        public bool EnableDefaultLighting { get; set; }

        // if false, TransformManager is disabled, so no built-in transform gizmo, etc
        public bool EnableTransforms { get; set; }

        // set this to your own gizmo builder to override default axis-transform gizmo
        public ITransformGizmoBuilder DefaultGizmoBuilder { get; set; }

        // if false, no cockpit is created (probably this doesn't work right now!)
        public bool EnableCockpit { get; set; }

        // If true, cockpit is rendered using an orthographic camera aligned with 3D scene cameras.
        // This allows for a standard 2D UI with depth-buffered layering, although the UI elements
        // can also be rotated in 3D.
        // FPlatform.UILayer specifies which camera layer is the UI layer.
        // SceneUIElements added to the Cockpit are automatically placed in this layer.
        // However, hit testing these elements requires using FContext.Find2DUIHit / FContext.Find2DUIHoverHit.
        //
        // One complication is that to prevnet each SceneUIElement from having to know it is in the ortho view,
        // InputState.vMouseWorldRay needs to be dynamically rewritten by the parent Behaviors.
        // See Mouse2DCockpitUIBehavior for an example.
        public bool Use2DCockpit { get; set; }

        public bool ConstantSize2DCockpit { get; set; }

        // this is called to set up the cockpit
        public ICockpitInitializer CockpitInitializer { get; set; }

        // initialize scene, eg with startup objects, etc. Is not clear that anyone is
        // using this right now...also possible to do manually in context setup object
        public ISceneInitializer SceneInitializer { get; set; }

        // camera-controls object for mouse and gamepad. Probably this will go away and
        // be replaced with Behaviors, that can be registered in cockpit setup
        public ICameraInteraction MouseCameraControls { get; set; }

        // for VR, this is an existing GameObject that contains stereo cameras,
        //   objects that provide tracked head/hand positions, etc
        public GameObject SpatialCameraRig { get; set; }


        // if true, we do not create/track an in-scene 3D cursor. Only
        // valid on desktop platform, no cursor on mobile and VR requires
        // in-scene 3D cursor.
        public bool UseSystemMouseCursor { get; set; }

        public SceneOptions()
        {
            EnableDefaultLighting = true;
            EnableCockpit = true;
            EnableTransforms = true;
            CockpitInitializer = null;
            SceneInitializer = null;
            MouseCameraControls = null;
            UseSystemMouseCursor = false;
            Use2DCockpit = false;
            ConstantSize2DCockpit = false;
            // default logging level is to be verbose in editor
            LogLevel = (Application.isEditor) ? 1 : 0;
        }
    }
}
