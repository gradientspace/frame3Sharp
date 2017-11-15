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
        public static Vector2f Center(IBoxModelElement element)         { return element.Bounds2D.Center; }
        public static Vector2f TopLeft(IBoxModelElement element)        { return element.Bounds2D.TopLeft; }
        public static Vector2f TopRight(IBoxModelElement element)       { return element.Bounds2D.TopRight; }
        public static Vector2f BottomLeft(IBoxModelElement element)     { return element.Bounds2D.BottomLeft; }
        public static Vector2f BottomRight(IBoxModelElement element)    { return element.Bounds2D.BottomRight; }
        public static Vector2f CenterLeft(IBoxModelElement element)     { return element.Bounds2D.CenterLeft; }
        public static Vector2f CenterRight(IBoxModelElement element)    { return element.Bounds2D.CenterRight; }
        public static Vector2f CenterTop(IBoxModelElement element)      { return element.Bounds2D.CenterTop; }
        public static Vector2f CenterBottom(IBoxModelElement element)   { return element.Bounds2D.CenterBottom; }


        public static Vector2f GetBoxPosition(IBoxModelElement element, BoxPosition pos)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            return GetBoxPosition(ref bounds, pos);
        }
        public static Vector2f GetBoxPosition(ref AxisAlignedBox2f bounds, BoxPosition pos)
        {
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


        public static AxisAlignedBox2f LocalBounds(IBoxModelElement element)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Translate(-bounds.Center);
            return bounds;
        }


        // This returns Bounds2D-Padding. Note that Bounds2D includes local translation 
        //  (ie relative to parent)
        public static FixedBoxModelElement PaddedBounds(IBoxModelElement element, float fPadding)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Contract(fPadding);
            return new FixedBoxModelElement(bounds);
        }

        // This returns Bounds2D-Padding. Note that Bounds2D includes local translation 
        //  (ie relative to parent)
        public static FixedBoxModelElement PaddedBounds(IBoxModelElement element, float fPadLeft, float fPadRight, float fPadBottom, float fPadTop)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Add(-fPadLeft, -fPadRight, -fPadBottom, -fPadTop);
            return new FixedBoxModelElement(bounds);
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
        public static FixedBoxModelElement ContentBounds(IBoxModelElement element, float fPadding)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Translate(-bounds.Center);
            return new FixedBoxModelElement(bounds);
        }


        // This returns ContentBounds with a padding offset
        public static FixedBoxModelElement PaddedContentBounds(IBoxModelElement element, float fPadding)
        {
            AxisAlignedBox2f bounds = element.Bounds2D;
            bounds.Translate(-bounds.Center);
            bounds.Contract(fPadding);
            return new FixedBoxModelElement(bounds);
        }


        // kind of feeling like this should maybe go somewhere else...
        public static Vector3f SetObjectPosition( IBoxModelElement element, Vector2f vObjectPoint, 
                                              Vector2f vTargetPoint, float z = 0)
        {
            IElementFrame item = element as IElementFrame;

            Vector2f vOffset = GetRelativeOffset(element, vObjectPoint);
            Vector2f vNewPos = vTargetPoint - vOffset;

            Vector3f vPos3 = new Vector3f(vNewPos.x, vNewPos.y, z);
            Frame3f f = new Frame3f(vPos3);
            item.SetObjectFrame(f);
            return vPos3;
        }


        // kind of feeling like this should maybe go somewhere else...
        public static Vector3f SetObjectPosition( IBoxModelElement element, BoxPosition objectPos, 
                                              Vector2f pos, float z = 0)
        {
            IElementFrame item = element as IElementFrame;

            Vector2f corner_offset = GetBoxOffset(element, objectPos);
            Vector2f new_pos = pos - corner_offset;

            Vector3f vPos3 = new Vector3f(new_pos.x, new_pos.y, z);
            Frame3f f = new Frame3f(vPos3);
            item.SetObjectFrame(f);
            return vPos3;
        }


        public static Vector3f SetObjectPosition( IBoxModelElement element, BoxPosition elemPos,
            IBoxModelElement relativeTo, BoxPosition relPos, 
            Vector2f vOffset, float z = 0)
        {
            Vector2f pos = GetBoxPosition(relativeTo, relPos);
            pos += vOffset;
            return SetObjectPosition(element, elemPos, pos, z);
        }



        public static void Translate(IBoxModelElement element, Vector2f delta, float z = 0)
        {
            IElementFrame item = element as IElementFrame;
            Frame3f f = item.GetObjectFrame();
            f.Origin += new Vector3f(delta.x, delta.y, z);
            item.SetObjectFrame(f);
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


        public static BoxPosition ToPosition(HorizontalAlignment h, VerticalAlignment v)
        {
            switch (h) {
                case HorizontalAlignment.Left:
                    switch (v) {
                        case VerticalAlignment.Bottom:  return BoxPosition.BottomLeft;
                        case VerticalAlignment.Center: return BoxPosition.CenterLeft;
                        case VerticalAlignment.Top: return BoxPosition.TopLeft;
                    }
                    return BoxPosition.Center;

                case HorizontalAlignment.Center:
                    switch (v) {
                        case VerticalAlignment.Bottom:  return BoxPosition.CenterBottom;
                        case VerticalAlignment.Center: return BoxPosition.Center;
                        case VerticalAlignment.Top: return BoxPosition.CenterTop;
                    }
                    return BoxPosition.Center;

                case HorizontalAlignment.Right:
                    switch (v) {
                        case VerticalAlignment.Bottom:  return BoxPosition.BottomRight;
                        case VerticalAlignment.Center: return BoxPosition.CenterRight;
                        case VerticalAlignment.Top: return BoxPosition.TopRight;
                    }
                    return BoxPosition.Center;
            }
            // shouldn't be able to get here...
            throw new Exception("BoxModel.ToPosition: unknown combination??");
        }


        public static BoxPosition ToBottom(BoxPosition p)
        {
            if (p == BoxPosition.TopLeft || p == BoxPosition.CenterLeft)
                return BoxPosition.BottomLeft;
            else if (p == BoxPosition.CenterTop || p == BoxPosition.Center)
                return BoxPosition.CenterBottom;
            else if (p == BoxPosition.TopRight || p == BoxPosition.CenterRight)
                return BoxPosition.BottomRight;
            return p;
        }

        public static BoxPosition ToTop(BoxPosition p)
        {
            if (p == BoxPosition.BottomLeft || p == BoxPosition.CenterLeft)
                return BoxPosition.TopLeft;
            else if (p == BoxPosition.CenterBottom || p == BoxPosition.Center)
                return BoxPosition.CenterTop;
            else if (p == BoxPosition.BottomRight || p == BoxPosition.CenterRight)
                return BoxPosition.TopRight;
            return p;
        }


    }



    /// <summary>
    /// just wraps a box as an IBoxModelElement
    /// </summary>
    public struct FixedBoxModelElement : IBoxModelElement
    {
        public AxisAlignedBox2f Box;
        public FixedBoxModelElement(AxisAlignedBox2f b) {
            Box = b;
        }

        public Vector2f Size2D
        {
            get { return Box.Diagonal; }
        }

        public AxisAlignedBox2f Bounds2D
        {
            get { return Box; }
        }
    }


}
