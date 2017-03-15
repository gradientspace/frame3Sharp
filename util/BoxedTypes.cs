using System;
using System.Collections.Generic;

namespace f3
{
    /// <summary>
    /// These are just object wrappers around POD types. This is useful
    /// in some cases where for example we want to pass a value type to
    /// to code that will hold a generic object reference. 
    /// 
    /// An example would be ActionSet, where you can register an Action<object>
    /// and the associated object instance that should be passed as the argument.
    /// The object could be something as simple as a state flag that the Action
    /// may want to update. 
    /// 
    /// An alternative is to box the type, eg like (object)value_type, but 
    /// as far as I can tell, it is not possible to update the value 'inside' the box.
    /// So, these can be used instead, and then the behavior is more explicit.
    /// </summary>



    public class BoxedBoolean
    {
        public bool Value = false;

        public BoxedBoolean(bool b) {
            Value = b;
        }

        public static implicit operator bool(BoxedBoolean b)
        {
            return b.Value;
        }
    }


    public class BoxedInt
    {
        public int Value = 0;

        public BoxedInt(int n) {
            Value = n;
        }

        public static implicit operator int(BoxedInt n)
        {
            return n.Value;
        }
    }


    public class BoxedFloat
    {
        public float Value = 0;

        public BoxedFloat(float f) {
            Value = f;
        }

        public static implicit operator float(BoxedFloat f)
        {
            return f.Value;
        }
    }



    public class BoxedDouble
    {
        public double Value = 0;

        public BoxedDouble(double f) {
            Value = f;
        }

        public static implicit operator double(BoxedDouble f)
        {
            return f.Value;
        }
    }


}
