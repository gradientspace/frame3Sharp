using UnityEngine;
using System;
using System.Collections;
using g3;


namespace f3 {

	public class SystemMouseCursorController : ICursorController
    {
		FContext context;

        protected Ray3f CurrentWorldRay;
        public Ray3f CurrentCursorWorldRay()
        {
            return CurrentWorldRay;
        }

        protected Ray3f CurrentUIRay;
        public Ray3f CurrentCursorOrthoRay()
        {
            return CurrentUIRay;
        }

        public bool HasSecondPosition { get { return false; } }
        public Ray3f SecondWorldRay() {
            throw new NotImplementedException("VRMouseCursorController.SecondWorldRay: not supported!");
        }

        public SystemMouseCursorController(FContext context)
        {
			this.context = context;
		}

		// Use this for initialization
		public void Start ()
        {
        }


		public void Update ()
        {
            // just convert current system mouse position into ray
            CurrentWorldRay = ((Camera)context.ActiveCamera).ScreenPointToRay(Input.mousePosition);
            CurrentUIRay = ((Camera)context.OrthoUICamera).ScreenPointToRay(Input.mousePosition);
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