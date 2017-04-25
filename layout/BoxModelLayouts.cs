using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    /// <summary>
    /// Implements standard 2.5D box layout based on Pin constraints
    /// </summary>
    public class BoxModel2DLayout : BoxContainerLayout
    {
        public BoxModel2DLayout(BoxContainer container) : base(container)
        {
        }

        protected override void layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            IBoxModelElement boxElem = e as IBoxModelElement;
            if (PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();
                BoxModel.SetObjectPosition(boxElem, SourcePos, PinToPos, pin.fZ);

            } else if (boxElem != null) {
                BoxModel.SetObjectPosition(boxElem, BoxPosition.Center, box.Center, 0);

            } else {
                // do nothing?
            }
        }
    }




    /// <summary>
    /// Implements 2.5D boxmodel-style layout on a 3D surface via IBoxModelRegion3D. 
    /// Basically this works the same way as normal 2D box layout, except that we
    /// map from/to the 3D surface first, via the 3D region.
    /// </summary>
    public class BoxModel3DRegionLayout : BoxContainerLayout
    {
        public IBoxModelRegion3D Region;


        public BoxModel3DRegionLayout(BoxContainer container, IBoxModelRegion3D region) : base(container)
        {
            Region = region;
        }


        protected override void layout_item(SceneUIElement e)
        {
            AxisAlignedBox2f box = Container.Bounds2D;

            IBoxModelElement boxElem = e as IBoxModelElement;
            IElementFrame eFramed = e as IElementFrame;

            if (PinConstraints.ContainsKey(e)) {
                Pin pin = PinConstraints[e];

                // evaluate pin constraints in 2D box space
                Vector2f SourcePos = pin.FromF();
                Vector2f PinToPos = pin.ToF();

                // map center of object into box space
                //  note: ignores orientation!
                Frame3f objF = eFramed.GetObjectFrame();
                Vector2f center2 = Region.To2DCoords(objF.Origin);

                // construct new 2D position
                Vector2f vOffset = SourcePos - center2;
                Vector2f vNewPos = PinToPos - vOffset;

                // map 2D position back to 3D surface and orient object
                Frame3f frame = Region.From2DCoords(vNewPos, pin.fZ);
                eFramed.SetObjectFrame(frame);

            } else if (boxElem != null) {

                // position object at center of box region
                Frame3f frame = Region.From2DCoords(Vector2f.Zero, 0);
                eFramed.SetObjectFrame(frame);


            } else {
                // do nothing?
            }
        }

    }



    






}
