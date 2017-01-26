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
        TopLeft = 4,

        CenterLeft = 5,
        CenterRight = 6,
        CenterTop = 7,
        CenterBottom = 8

    }


    public class BoxModel
    {


        public static Vector2f GetBoundsPosition(IBoxModelElement element, BoxPosition pos)
        {
            switch (pos) {
                case BoxPosition.Center: return element.Bounds2D.Center;
                case BoxPosition.BottomLeft: return element.Bounds2D.BottomLeft;
                case BoxPosition.BottomRight: return element.Bounds2D.BottomRight;
                case BoxPosition.TopRight: return element.Bounds2D.TopRight;
                case BoxPosition.TopLeft: return element.Bounds2D.TopLeft;

                case BoxPosition.CenterLeft: return element.Bounds2D.CenterLeft;
                case BoxPosition.CenterRight: return element.Bounds2D.CenterRight;
                case BoxPosition.CenterTop: return element.Bounds2D.CenterTop;
                case BoxPosition.CenterBottom: return element.Bounds2D.CenterBottom;

                default: return element.Bounds2D.Center;
            }
        }


        // kind of feeling like this should maybe go somewhere else...
        public static void SetObjectPosition( IBoxModelElement element, BoxPosition objectPos, 
                                              Vector2f pos, float z = 0)
        {
            Vector2f corner = GetBoundsPosition(element, objectPos);

            // [RMS] this is true for now...need to rethink though
            HUDStandardItem item = element as HUDStandardItem;

            Frame3f f = new Frame3f(new Vector3f(pos.x-corner.x, pos.y-corner.y, z));
            item.SetObjectFrame(f);
        }



        public static void Translate(fGameObject go, Vector2f from, Vector2f to, float z = 0)
        {
            Vector2f dv = to - from;
            go.Translate(new Vector3f(dv.x, dv.y, z));
        }

    }
}
