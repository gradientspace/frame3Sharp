using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    public enum HorizontalAlignment {
        Left = 0,
        Center = 1,
        Right = 2
    }
    public enum VerticalAlignment {
        Bottom = 0,
        Center = 1,
        Top = 2
    }

    public delegate void BoundsModifiedEventHandler(object sender);


    // provides 2D interface we can use for 2D layout
    public interface IBoxModelElement
    {
        // this is the dimension of the element
        Vector2f Size2D { get; }

        // this is the bounds of the element in its local coordinate system, ie relative to parent
        AxisAlignedBox2f Bounds2D { get; }
    }

}
