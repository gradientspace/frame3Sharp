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

        void HideCursor();
        void ShowCursor();

        void ResetCursorToCenter();


        Vector3f CurrentWorldPosition();
        Vector3f CurrentRaySourceWorldPosition();
        Ray3f CurrentCursorWorldRay();
    }
}
