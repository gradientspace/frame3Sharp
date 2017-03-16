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


        public static Vector2f GetBoxPosition(IBoxModelElement element, BoxPosition pos)
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

        public static Vector2f GetBoxOffset(IBoxModelElement element, BoxPosition boxPos)
        {
            return GetBoxPosition(element, boxPos) - element.Bounds2D.Center;
        }


        /// <summary>
        /// returns offset of vPosition from center of element
        /// </summary>
        public static Vector2f GetRelativeOffset(IBoxModelElement element, Vector2f vPosition)
        {
            return vPosition - element.Bounds2D.Center;
        }


        // This returns Bounds2D-Padding. Note that Bounds2D includes local translation 
        //  (ie relative to parent)
        public static AxisAlignedBox2f PaddedBounds(IBoxModelElement element, float fPadding)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Contract(fPadding);
            return bounds;
        }

        // This returns Size-Padding
        public static Vector2f PaddedSize(IBoxModelElement element, float fPadding)
        {
            Vector2f size = element.Size2D;
            size -= 2*fPadding;
            return size;
        }


        // This returns element.Bounds2D re-centered at origin, ie without the local
        // translation of element. Child elements should be laid out relative to this box.
        public static AxisAlignedBox2f ContentBounds(IBoxModelElement element, float fPadding)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Translate(-bounds.Center);
            return bounds;
        }


        // This returns ContentBounds with a padding offset
        public static AxisAlignedBox2f PaddedContentBounds(IBoxModelElement element, float fPadding)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Translate(-bounds.Center);
            bounds.Contract(fPadding);
            return bounds;
        }


        // kind of feeling like this should maybe go somewhere else...
        public static void SetObjectPosition( IBoxModelElement element, Vector2f vObjectPoint, 
                                              Vector2f vTargetPoint, float z = 0)
        {
            // [RMS] this is true for now...need to rethink though
            HUDStandardItem item = element as HUDStandardItem;

            Vector2f vOffset = GetRelativeOffset(element, vObjectPoint);
            Vector2f vNewPos = vTargetPoint - vOffset;

            Frame3f f = new Frame3f(new Vector3f(vNewPos.x, vNewPos.y, z));
            item.SetObjectFrame(f);
        }


        // kind of feeling like this should maybe go somewhere else...
        public static void SetObjectPosition( IBoxModelElement element, BoxPosition objectPos, 
                                              Vector2f pos, float z = 0)
        {
            // [RMS] this is true for now...need to rethink though
            HUDStandardItem item = element as HUDStandardItem;

            Vector2f corner_offset = GetBoxOffset(element, objectPos);
            Vector2f new_pos = pos - corner_offset;

            Frame3f f = new Frame3f(new Vector3f(new_pos.x, new_pos.y, z));
            item.SetObjectFrame(f);
        }


        public static void SetObjectPosition( IBoxModelElement element, BoxPosition elemPos,
            IBoxModelElement relativeTo, BoxPosition relPos, 
            Vector2f vOffset, float z = 0)
        {
            Vector2f pos = GetBoxPosition(relativeTo, relPos);
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
