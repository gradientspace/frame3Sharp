using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public enum ToolTargetType {
        SingleObject, MultipleObject, Scene
    }


    public interface ITool : InputBehaviorSource
    {
        string Name { get; }
        string TypeIdentifier { get; }

        // inherited from from InputBehaviorSource
        //InputBehaviorSet InputBehaviors { get; }

        // called on per-frame Update()
        void PreRender();

        // not all tools have an Apply action, but some do
        bool HasApply { get; }
        bool CanApply { get; }
        void Apply();

        // if false, cannot change selection while tool is active
        bool AllowSelectionChanges { get; }

        void Setup();      // called right after constructor
        void Shutdown();   // called right before deactivating
    }


    public interface IToolBuilder
    {
        bool IsSupported(ToolTargetType type, List<SceneObject> targets);
        ITool Build(FScene scene, List<SceneObject> targets);
    }
}
