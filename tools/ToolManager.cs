using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{

    public delegate void ToolActivationChangedEvent(ITool tool, ToolSide eSide, bool bActivated);


    public enum ToolSide
    {
        Left = 0, Right = 1
    }

    public class ToolManager
    {
        public FContext SceneManager { get; set; }

        Dictionary<string, IToolBuilder> ToolTypes;

        string[] activeType = { "", "" };
        IToolBuilder[] activeBuilder = { null, null };
        ITool[] activeTool = { null, null };

        public ToolManager()
        {
            //activeBuilder = new DrawPrimitivesToolBuilder();
            //activeGizmo = null;

            ToolTypes = new Dictionary<string, IToolBuilder>();

            // default tool? no
            //RegisterToolType(DrawPrimitivesTool.Identifier, new DrawPrimitivesToolBuilder());
            //SetActiveToolType(DrawPrimitivesTool.Identifier, ToolSide.Left);
            //SetActiveToolType(DrawPrimitivesTool.Identifier, ToolSide.Right);
        }


        public void Initialize(FContext manager)
        {
            SceneManager = manager;
            //SceneManager.Scene.SelectionChangedEvent += Scene_SelectionChangedEvent;
        }



        public void RegisterToolType(string sType, IToolBuilder builder)
        {
            if (ToolTypes.ContainsKey(sType))
                throw new ArgumentException("ToolManager.RegisterToolType : type " + sType + " already registered!");
            ToolTypes[sType] = builder;
        }



        public ITool ActiveLeftTool {
            get { return activeTool[0]; }
        }
        public ITool ActiveRightTool {
            get { return activeTool[1]; }
        }
        public ITool GetActiveTool(ToolSide eSide) {
            return activeTool[(int)eSide];
        }
        public ITool GetActiveTool(int nSide) {
            return activeTool[nSide];
        }
        public bool HasActiveTool(ToolSide eSide) {
            return activeTool[(int)eSide] != null;
        }
        public bool HasActiveTool(int nSide) {
            return activeTool[nSide] != null;
        }

        public void SetActiveToolType(string sType, int nSide) {
            SetActiveToolType(sType, (nSide == 0) ? ToolSide.Left : ToolSide.Right);
        }
        public void SetActiveToolType(string sType, ToolSide eSide)
        {
            if ( ToolTypes.ContainsKey(sType) == false )
                throw new ArgumentException("Toolmanager.SetActiveToolType : type " + sType + " is not registered!");

            int nSide = (int)eSide;
            if (activeType[nSide] == sType)
                return;

            activeType[nSide] = sType;
            activeBuilder[nSide] = ToolTypes[sType];

            if ( activeTool[nSide] != null) {
                DeactivateTool(eSide);
            }
        }


        public bool ActivateTool(int nSide) {
           return ActivateTool((nSide == 0) ? ToolSide.Left : ToolSide.Right);
        }
        public bool ActivateTool(ToolSide eSide)
        {
            int nSide = (int)eSide;
            if (activeType[nSide] == null || activeBuilder[nSide] == null)
                return false;

            // deactivate existing tool? I guess.
            if ( activeTool[nSide] != null ) 
                DeactivateTool(eSide);
                

            List<SceneObject> selected = new List<SceneObject>(SceneManager.Scene.Selected);
            if (selected.Count > 1) {
                if (activeBuilder[nSide].IsSupported(ToolTargetType.MultipleObject, selected))
                    activeTool[nSide] = activeBuilder[nSide].Build(SceneManager.Scene, selected);
            } else if ( selected.Count == 1 ) {
                if (activeBuilder[nSide].IsSupported(ToolTargetType.SingleObject, selected))
                    activeTool[nSide] = activeBuilder[nSide].Build(SceneManager.Scene, selected);
            }

            if ( activeTool[nSide] == null ) {
                if ( activeBuilder[nSide].IsSupported(ToolTargetType.Scene, null) )
                    activeTool[nSide] = activeBuilder[nSide].Build(SceneManager.Scene, null);
            }

            if (activeTool[nSide] != null) {
                SendOnToolActivationChanged(activeTool[nSide], eSide, true);
                return true;
            } else
                return false;
        }


        public void DeactivateTool(int nSide) {
            DeactivateTool((nSide == 0) ? ToolSide.Left : ToolSide.Right);
        }
        public void DeactivateTool(ToolSide eSide)
        {
            int nSide = (int)eSide;
            if (activeTool[nSide] != null) {
                // shutdown tool
                ITool tool = activeTool[nSide];
                tool.Shutdown();
                activeTool[nSide] = null;
                SendOnToolActivationChanged(tool, eSide, false);
            }
        }


        public event ToolActivationChangedEvent OnToolActivationChanged;
        protected virtual void SendOnToolActivationChanged(ITool tool, ToolSide eSide, bool bActivated)
        {
            var tmp = OnToolActivationChanged;
            if (tmp != null)
                tmp(tool, eSide, bActivated);
        }



        public void PreRender()
        {
            for ( int k = 0; k < 2; ++k ) {
                if (activeTool[k] != null)
                    activeTool[k].PreRender();
            }
        }



    }
}
