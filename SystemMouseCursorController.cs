using UnityEngine;
using System;
using System.Collections;
using g3;


namespace f3 {

	public class SystemMouseCursorController : ICursorController
    {
		Camera camera;
		FContext context;

        protected Ray3f CurrentWorldRay;
        public Ray3f CurrentCursorWorldRay()
        {
            return CurrentWorldRay;
        }


        public SystemMouseCursorController(Camera viewCam, FContext context)
        {
			camera = viewCam;
			this.context = context;
		}

		// Use this for initialization
		public void Start ()
        {
        }


		public void Update ()
        {
            // just convert current system mouse position into ray
            CurrentWorldRay = camera.ScreenPointToRay(Input.mousePosition);
        }


        public void ResetCursorToCenter()
        {
            // invalid for system cursor
        }
        public void HideCursor()
        {
            // invalid for system cursor
        }
        public void ShowCursor()
        {
            // invalid for system cursor
        }


	}

}