using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public enum CenterModes
    {
        Origin,         // mesh is centered at object center
        Base,           // x/z at object center, y at bottom
        Corner          // x/y/z at min-corner of bbox
    }


    //
    // This can be used as a base class for parametric primitive SOs, like
    //  Cylinders, Cubes, etc, where the paramters of the shape need to be exposed
    public abstract class PrimitiveSO : BaseSO
    {
        ParameterSet parameters;

        // [RMS] implementors can use this flag to avoid expensive operations while
        //   user is editing primitive (eg like recomputing spatial data structures).
        //   Anything doing primitive editing interactively (eg like on mouse-drag)
        //   should be setting DeferRebuild to true/false during the interactive operation
        bool defer_rebuild;


        CenterModes centerMode = CenterModes.Base;
        public CenterModes Center
        {
            get { return centerMode; }
            set { if (centerMode != value) { centerMode = value; UpdateGeometry(); } }
        }


        public PrimitiveSO() : base()
        {
            parameters = new ParameterSet();

            defer_rebuild = false;
            Parameters.Register(
                "defer_rebuild", () => { return DeferRebuild; }, (b) => { DeferRebuild = b; }, defer_rebuild);
        }


        public override bool IsSurface {
            get { return true; }
        }

        public ParameterSet Parameters {
            get { return parameters; }
        }

        virtual public bool DeferRebuild
        {
            get { return defer_rebuild; }
            set {
                bool rebuild = (defer_rebuild == true && value == false);
                defer_rebuild = value;
                if (rebuild)
                    UpdateGeometry();
            }
        }


        // PrimitiveSO interface that subclasses must implement
        abstract public void UpdateGeometry();

    }
}
