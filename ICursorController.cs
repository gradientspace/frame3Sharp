using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{
    public interface ICursorController
    {
        void Start();

        void Update();

        // these are only applicable in some cases...
        void HideCursor();
        void ShowCursor();
        void ResetCursorToCenter();

        Ray3f CurrentCursorWorldRay();
        Ray3f CurrentCursorOrthoRay();

        bool HasSecondPosition { get; }
        Ray3f SecondWorldRay();
    }
}
