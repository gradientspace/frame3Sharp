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

        public bool EnableTransforms { get; set; }

        public bool EnableCockpit { get; set; }
        public ICockpitInitializer CockpitInitializer { get; set; }

        public ISceneInitializer SceneInitializer { get; set; }

        public ICameraInteraction MouseCameraControls { get; set; }

        public GameObject SpatialCameraRig { get; set; }

        public bool UseSystemMouseCursor { get; set; }

        public SceneOptions()
        {
            EnableCockpit = true;
            EnableTransforms = true;
            CockpitInitializer = null;
            SceneInitializer = null;
            MouseCameraControls = null;
            UseSystemMouseCursor = false;
            // default logging level is to be verbose in editor
            LogLevel = (Application.isEditor) ? 1 : 0;
        }
    }
}
