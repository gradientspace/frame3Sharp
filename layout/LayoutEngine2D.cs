using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    /// <summary>
    /// Basic 2.5D cockpit UI layout engine, built on top of HUDContainerLayout
    /// Quirks:
    ///   - automatically uses fade in/out transitions on Add/Remove
    /// </summary>
    public class LayoutEngine2D : ILayoutEngine
    {
        public Cockpit Cockpit;
        public HUDContainer ScreenContainer;
        public HUDContainerLayout Layout;

        public float StandardDepth;


        List<SceneUIElement> elements;

        public LayoutEngine2D(Cockpit parent)
        {
            this.Cockpit = parent;
            ScreenContainer = new HUDContainer(new Cockpit2DContainerProvider(parent));
            Layout = new HUDContainerLayout(ScreenContainer);

            elements = new List<SceneUIElement>();
        }



        public float UIScaleFactor
        {
            get { return Cockpit.GetPixelScale(); }
        }
        public IBoxModelElement BoxModelContainer
        {
            get { return this.ScreenContainer; }
        }


        public void Add(SceneUIElement element, LayoutOptions options)
        {
            if ( elements.Contains(element) ) 
                throw new Exception("LayoutEngine2D.Add: element is already in layout");
            if ( element is HUDStandardItem == false )
                throw new Exception("LayoutEngine2D.Add: element must be a HUDStandardItem");
            if ( element is IBoxModelElement == false ) 
                throw new Exception("LayoutEngine2D.Add: element must implement IBoxModelElement");

            elements.Add(element);
            Add(element as HUDStandardItem, options);
        }

        public void RemoveAll()
        {
            foreach (var e in elements)
                Remove(e);
        }


        public void Remove(SceneUIElement element)
        {
            if ( elements.Contains(element) == false ) 
                throw new Exception("LayoutEngine2D.Remove: element is not in layout");
            if ( element is HUDStandardItem == false )
                throw new Exception("LayoutEngine2D.Add: element must be a HUDStandardItem");

            elements.Remove(element);

            // this will remove from cockpit after transition
            HUDUtil.AnimatedDimiss_Cockpit(element as HUDStandardItem, this.Cockpit);
        }




        public void Add(HUDStandardItem element, LayoutOptions options)
        {
            IBoxModelElement elemBoxModel = element as IBoxModelElement;

            Frame3f viewFrame = Cockpit.GetViewFrame2D();
            //AxisAlignedBox2f uiBounds = Cockpit.GetOrthoViewBounds();
            //float pixelScale = Cockpit.GetPixelScale();

            HUDUtil.PlaceInViewPlane(element, viewFrame);
            Cockpit.AddUIElement(element);

            Func<Vector2f> pinSourceF = options.PinSourcePoint2D;
            if (pinSourceF == null)
                pinSourceF = HUDLayoutUtil.BoxPointF(elemBoxModel, BoxPosition.Center);

            Func<Vector2f> pinTargetF = options.PinTargetPoint2D;
            if (pinTargetF == null)
                pinTargetF = HUDLayoutUtil.BoxPointF(ScreenContainer, BoxPosition.Center);

            Layout.AddLayoutItem(element, pinSourceF, pinTargetF, this.StandardDepth + options.DepthShift);

            // auto-show
            HUDUtil.AnimatedShow(element);
        }






    }
}
