using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public class HUDUIDefaults
    {
        public static float UIScale = 1.0f;

        public static Func<float, float, HUDShape> MakeStandardButtonShapeF = (width, height) => {
            return new HUDShape(HUDShapeType.Rectangle, width, height, 0, 1, false);
        };

        public static Func<float, float, HUDShape> MakeDialogBackgroundShapeF = (width, height) => {
            return new HUDShape(HUDShapeType.Rectangle, width, height, 0, 1, false);
        };

        public static Func<float, float, HUDShape> MakeDialogButtonShapeF = (width, height) => {
            return new HUDShape(HUDShapeType.Rectangle, width, height, 0, 1, false);
        };

    }
}
