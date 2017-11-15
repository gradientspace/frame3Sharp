using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    public static class LayoutUtil
    {

        public static LayoutOptions PointToPoint(IBoxModelElement eFrom, BoxPosition posFrom, 
                                                 IBoxModelElement eTo, BoxPosition posTo, float fShiftZ = 0)
        {
            return new LayoutOptions() {
                Flags = LayoutFlags.None,
                PinSourcePoint2D = BoxPointF(eFrom, posFrom),
                PinTargetPoint2D = BoxPointF(eTo, posTo),
                DepthShift = fShiftZ
            }; 
        }


        public static LayoutOptions PointToPoint(IBoxModelElement eFrom, BoxPosition posFrom, Vector2f vFromDelta, 
                                                 IBoxModelElement eTo, BoxPosition posTo, float fShiftZ = 0)
        {
            return new LayoutOptions() {
                Flags = LayoutFlags.None,
                PinSourcePoint2D = BoxPointF(eFrom, posFrom, vFromDelta),
                PinTargetPoint2D = BoxPointF(eTo, posTo),
                DepthShift = fShiftZ
            }; 
        }


        public static LayoutOptions PointToPoint(IBoxModelElement eFrom, BoxPosition posFrom, 
                                                 IBoxModelElement eTo, BoxPosition posTo, Vector2f vToDelta, float fShiftZ = 0)
        {
            return new LayoutOptions() {
                Flags = LayoutFlags.None,
                PinSourcePoint2D = BoxPointF(eFrom, posFrom),
                PinTargetPoint2D = BoxPointF(eTo, posTo, vToDelta),
                DepthShift = fShiftZ
            }; 
        }


        public static LayoutOptions PointToPointSplitXY(IBoxModelElement eFrom, BoxPosition posFrom, 
                                                        IBoxModelElement eToX, BoxPosition posToX, 
                                                        IBoxModelElement eToY, BoxPosition posToY, 
                                                        Vector2f vToDelta, float fShiftZ = 0)
        {
            return new LayoutOptions() {
                Flags = LayoutFlags.None,
                PinSourcePoint2D = BoxPointF(eFrom, posFrom),
                PinTargetPoint2D = BoxPointSplitXYF(eToX, posToX, eToY, posToY, vToDelta),
                DepthShift = fShiftZ
            }; 
        }



        public static LayoutOptions PointToPoint(IBoxModelElement eFrom, BoxPosition posFrom, Vector2f vFromDelta, 
                                                 IBoxModelElement eTo, BoxPosition posTo, Vector2f vToDelta, float fShiftZ = 0)
        {
            return new LayoutOptions() {
                Flags = LayoutFlags.None,
                PinSourcePoint2D = BoxPointF(eFrom, posFrom, vFromDelta),
                PinTargetPoint2D = BoxPointF(eTo, posTo, vToDelta),
                DepthShift = fShiftZ
            }; 
        }



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
        /// Constructs func that returns 2D point in origin-centered box (rather than current position)
        /// </summary>
        public static Func<Vector2f> LocalBoxPointF(IBoxModelElement element, BoxPosition pos)
        {
            Func<Vector2f> f = () => {
                Vector2f size = element.Size2D;
                AxisAlignedBox2f box = new AxisAlignedBox2f(Vector2f.Zero, size.x*0.5f, size.y * 0.5f);
                return BoxModel.GetBoxPosition(ref box, pos);
            };
            return f;
        }


        /// <summary>
        /// constructs func that returns box point in 2D layout space of the passed solver
        /// (eg if laying out on a 3D plane, for example)
        /// </summary>
        public static Func<Vector2f> InLayoutSpaceBoxPointF(IBoxModelElement element, BoxPosition pos, PinnedBoxesLayoutSolver solver, Vector2f vDelta)
        {
            Func<Vector2f> f = () => {
                Vector2f solverPos = solver.GetLayoutCenter(element as SceneUIElement);
                Vector2f size = element.Size2D;
                AxisAlignedBox2f box = new AxisAlignedBox2f(solverPos, size.x * 0.5f, size.y * 0.5f);
                return BoxModel.GetBoxPosition(ref box, pos) + vDelta;
            };
            return f;
        }


        /// <summary>
        /// constructs a Func that returns a 2D point
        /// </summary>
        public static Func<Vector2f> BoxPointSplitXYF(IBoxModelElement elementX, BoxPosition posX, IBoxModelElement elementY, BoxPosition posY, Vector2f vDelta) {
            Func<Vector2f> f = () => {
                Vector2f x = BoxModel.GetBoxPosition(elementX, posX);
                Vector2f y = BoxModel.GetBoxPosition(elementY, posY);
                return new Vector2f(x.x + vDelta.x, y.y + vDelta.y);
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
