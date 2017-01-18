using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public enum BoxPosition
    {
        Center = 0,
        BottomLeft = 1,
        BottomRight = 2,
        TopRight = 3,
        TopLeft = 4
    }


    public class BoxModel
    {


        public static Vector2f GetBoundsPosition(IBoxModelElement element, BoxPosition pos)
        {
            return (pos == 0) ? element.Bounds2D.Center : element.Bounds2D.GetCorner((int)pos + 1);
        }


        // kind of feeling like this should maybe go somewhere else...
        public static void SetObjectPosition( IBoxModelElement element, BoxPosition objectPos, 
                                              Vector2f pos, float z = 0)
        {
            Vector2f corner = GetBoundsPosition(element, objectPos);

            // [RMS] this is true for now...need to rethink though
            HUDStandardItem item = element as HUDStandardItem;

            Frame3f f = new Frame3f(new Vector3f(pos.x+corner.x, pos.y+corner.y, z));
            item.SetObjectFrame(f);
        }
    }
}
