using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    // provides 2D interface we can use for 2D layout
    public interface IBoxModelElement
    {
        // this is the dimension of the element
        Vector2f Size2D { get; }

        // this is the bounds of the element in its local coordinate system
        // (usually [-size/2, size/2]
        AxisAlignedBox2f Bounds2D { get; }
    }

}
