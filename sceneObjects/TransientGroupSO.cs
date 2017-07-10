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

        public override bool IsSelectable { 
            get { return false; }
        }

        override public SceneObject Duplicate()
        {
            throw new InvalidOperationException("TransientXFormSO::Duplicate not implemented!");
        }


        public TransientGroupSO()
        {
            SelectionMode = SelectionModes.SelectChildren;
        }
    }
}
