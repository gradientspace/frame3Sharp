using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public static class HUDLayoutUtil
    {

        /// <summary>
        /// constructs a Func that returns a 2D point
        /// </summary>
        public static Func<Vector2f> BoxPointF(IBoxModelElement element, BoxPosition pos) {
            Func<Vector2f> f = () => {
                return BoxModel.GetBoxPosition(element, pos);
            };
            return f;
        }


        /// <summary>
        /// constructs a Func that returns a 2D point
        /// </summary>
        public static Func<Vector2f> BoxPointInterpF(IBoxModelElement element, BoxPosition pos1, BoxPosition pos2, float alpha) {
            Func<Vector2f> f = () => {
                Vector2f p1 = BoxModel.GetBoxPosition(element, pos1);
                Vector2f p2 = BoxModel.GetBoxPosition(element, pos2);
                return Vector2f.Lerp(p1, p2, alpha);
            };
            return f;
        }


        /// <summary>
        /// constructs a Func that returns a 2D point + 2D offset
        /// </summary>
        public static Func<Vector2f> BoxPointF(IBoxModelElement element, BoxPosition pos, Vector2f vDelta) {
            Func<Vector2f> f = () => {
                return BoxModel.GetBoxPosition(element, pos) + vDelta;
            };
            return f;
        }

    }
}
