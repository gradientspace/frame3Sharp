using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public class ColorUtil
    {
        // create float color from bytes
        static public Colorf make(int r, int g, int b, int a = 255)
        {
            return new Colorf(
                MathUtil.Clamp((float)r, 0.0f, 255.0f) / 255.0f,
                MathUtil.Clamp((float)g, 0.0f, 255.0f) / 255.0f,
                MathUtil.Clamp((float)b, 0.0f, 255.0f) / 255.0f,
                MathUtil.Clamp((float)a, 0.0f, 255.0f) / 255.0f);
        }

        static public Colorf replaceAlpha(Colorf c, float a)
        {
            return new Colorf(c.r, c.g, c.b, a);
        }

        static public Colorf Lighten(Colorf c, float fAmount)
        {
            c[0] = MathUtil.Clamp(c[0] + fAmount, 0.0f, 1.0f);
            c[1] = MathUtil.Clamp(c[1] + fAmount, 0.0f, 1.0f);
            c[2] = MathUtil.Clamp(c[2] + fAmount, 0.0f, 1.0f);
            return c;
        }

        // standard color types

        // TODO video-safe RGB colors

        static readonly public Colorf ForestGreen = make(34, 139, 34);
        static readonly public Colorf CgRed = make(224, 60, 49);


        static readonly public Colorf MiddleGrey = make(128,128,128);
        static readonly public Colorf DarkGrey = make(64, 64, 64);


        static readonly public Colorf StandardBeige = new Colorf(0.75f, 0.75f, 0.5f);
        static readonly public Colorf SelectionGold = new Colorf(1.0f, 0.6f, 0.05f);
        static readonly public Colorf PivotYellow = new Colorf(1.0f, 1.0f, 0.05f);
    }
}
