using System;

namespace f3
{
    // pre-defind / utility stuff for mouse behaviors
    public static class MouseBehaviors
    {
        public static Func<InputState, bool> LeftButtonPressedF = 
            (input) => { return input.bLeftMousePressed; };

        public static Func<InputState, bool> LeftButtonDownF = 
            (input) => { return input.bLeftMouseDown; };

        public static Func<InputState, bool> LeftButtonReleasedF = 
            (input) => { return input.bLeftMouseReleased; };


        public static Func<InputState, bool> MiddleButtonPressedF = 
            (input) => { return input.bMiddleMousePressed; };

        public static Func<InputState, bool> MiddleButtonDownF = 
            (input) => { return input.bMiddleMouseDown; };

        public static Func<InputState, bool> MiddleButtonReleasedF = 
            (input) => { return input.bMiddleMouseReleased; };


        public static Func<InputState, bool> RightButtonPressedF = 
            (input) => { return input.bRightMousePressed; };

        public static Func<InputState, bool> RightButtonDownF = 
            (input) => { return input.bRightMouseDown; };

        public static Func<InputState, bool> RightButtonReleasedF = 
            (input) => { return input.bRightMouseReleased; };


    }
}
