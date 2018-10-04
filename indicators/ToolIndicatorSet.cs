using System;
using System.Collections.Generic;
using g3;

namespace f3
{

    public class ToolIndicatorSet : IndicatorSet
    {
        ITool parentTool;

        fGameObject worldParentGO;
        fGameObject sceneParentGO;

        public ToolIndicatorSet(ITool tool, FScene scene) : base(scene)
        {
            parentTool = tool;
            Initialize();
        }

        public virtual void Initialize()
        {
            worldParentGO = GameObjectFactory.CreateParentGO(parentTool.Name + "_indicators_world");
            sceneParentGO = GameObjectFactory.CreateParentGO(parentTool.Name + "_indicators_scene");
            UnityUtil.AddChild(Scene.TransientObjectsParent, sceneParentGO, false);
        }

        public override void AddIndicator(Indicator i)
        {
            base.AddIndicator(i);
            CoordSpace eSpace = i.InSpace;
            if (eSpace == CoordSpace.ObjectCoords)
                throw new Exception("ToolIndicatorSet.AddIndicator: object-space indicators not supported!");
            if (eSpace == CoordSpace.SceneCoords)
                UnityUtil.AddChild(sceneParentGO, i.RootGameObject, false);
            else
                UnityUtil.AddChild(worldParentGO, i.RootGameObject, true);
        }


        public override void Disconnect(bool bDestroy)
        {
            sceneParentGO.SetParent(null);

            base.Disconnect(bDestroy);

            if (bDestroy) {
                sceneParentGO.Destroy();
                worldParentGO.Destroy();
            } 
        }

    }
}
