using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public abstract class Indicator
    {
        public abstract fGameObject RootGameObject { get; }

        public abstract CoordSpace InSpace { get; }
        public abstract bool IsVisible { get; }

        public abstract void Setup();       // Called by ToolIndicatorSet on Add()
        public abstract void PreRender();
        public abstract void Destroy();
    }


    public class IndicatorSet
    {
        protected List<Indicator> Indicators = new List<Indicator>();

        protected FScene Scene;
        protected PreRenderHelper preRender;


        public IndicatorSet(FScene scene)
        {
            Scene = scene;

            preRender = new PreRenderHelper("indicators_helper") {
                PreRenderF = () => { this.PreRender(); }
            };
            scene.AddUIElement(preRender);
        }


        public virtual void AddIndicator(Indicator i)
        {
            i.Setup();
            Indicators.Add(i);
        }




        public virtual void Disconnect(bool bDestroy)
        {
            if (bDestroy) {
                foreach (var i in Indicators) {
                    i.RootGameObject.SetParent(null);
                    i.Destroy();
                }
                if (preRender != null)
                    Scene.RemoveUIElement(preRender, true);
            } else {
                if (preRender != null)
                    Scene.RemoveUIElement(preRender, false);
            }
        }


        public virtual void ClearAllIndicators()
        {
            foreach (Indicator id in Indicators)
                id.Destroy();
            Indicators.Clear();
        }


        public virtual void SetLayer(Indicator i, int nLayer)
        {
            i.RootGameObject.SetLayer(nLayer);
        }



        public void PreRender()
        {
            foreach (var i in Indicators) {
                if (i.IsVisible == false) {
                    if (i.RootGameObject.IsVisible())
                        i.RootGameObject.Hide();
                } else {
                    if (i.RootGameObject.IsVisible() == false)
                        i.RootGameObject.Show();

                    i.PreRender();
                }
            }

        }



    }
}
