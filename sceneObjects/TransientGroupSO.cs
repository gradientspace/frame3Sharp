using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    class TransientGroupSO : GroupSO
    {

        public override bool IsTemporary
        {
            get { return true; }
        }

        override public SceneObject Duplicate()
        {
            throw new InvalidOperationException("TransientXFormSO::Duplicate not implemented!");
        }
    }
}
