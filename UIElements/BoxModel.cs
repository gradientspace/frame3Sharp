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
            AxisAlignedBox2f bounds = element.Bounds2D;

            switch (pos) {
                case BoxPosition.Center: return bounds.Center;
                case BoxPosition.BottomLeft: return bounds.BottomLeft;
                case BoxPosition.BottomRight: return bounds.BottomRight;
                case BoxPosition.TopRight: return bounds.TopRight;
                case BoxPosition.TopLeft: return bounds.TopLeft;

                case BoxPosition.CenterLeft: return bounds.CenterLeft;
                case BoxPosition.CenterRight: return bounds.CenterRight;
                case BoxPosition.CenterTop: return bounds.CenterTop;
                case BoxPosition.CenterBottom: return bounds.CenterBottom;

                default: return bounds.Center;
            }
        }


        public static AxisAlignedBox2f PaddedBounds(IBoxModelElement element, float fPadding)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Contract(fPadding);
            return bounds;
        }

        public static Vector2f PaddedSize(IBoxModelElement element, float fPadding)
        {
            Vector2f size = element.Size2D;
            size -= 2*fPadding;
            return size;
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


        public static void SetObjectPosition( IBoxModelElement element, BoxPosition elemPos,
            IBoxModelElement relativeTo, BoxPosition relPos, 
            Vector2f vOffset, float z = 0)
        {
            Vector2f pos = GetBoundsPosition(relativeTo, relPos);
            pos += vOffset;
            SetObjectPosition(element, elemPos, pos, z);
        }


        public static void Translate(fGameObject go, Vector2f from, Vector2f to, float z = 0)
        {
            Vector2f dv = to - from;
            Translate(go, dv, z);
        }

        public static void Translate(fGameObject go, Vector2f delta, float z = 0)
        {
            Vector3f cur = go.GetLocalPosition();
            cur.x += delta.x; cur.y += delta.y; cur.z += z;
            go.SetLocalPosition(cur);
        }
        public static void MoveTo(fGameObject go, Vector2f to, float z = 0)
        {
            go.SetLocalPosition(new Vector3f(to.x, to.y, z));
        }

    }
}
