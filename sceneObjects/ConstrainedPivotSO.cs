using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    class ConstrainedPivotSO : PivotSO
    {
        public Func<Frame3f, Frame3f> ConstraintF = (x) => { return x; };

        public CoordSpace ConstraintSpace = CoordSpace.WorldCoords;


        override public void SetLocalFrame(Frame3f newFrame, CoordSpace eSpace) {

            base.SetLocalFrame(newFrame, eSpace);

        }



    }
}
