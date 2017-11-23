using UnityEngine;
using System;
using System.Collections;
using g3;


namespace f3 {

	public class TouchMouseCursorController : ICursorController
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


        protected bool secondWorldRayValid;
        protected Ray3f secondWorldRay;

        public bool HasSecondPosition {
            get { return secondWorldRayValid; }
        }
        public Ray3f SecondWorldRay()
        {
            return secondWorldRay;
        }

        public TouchMouseCursorController(FContext context)
        {
			this.context = context;
		}

		// Use this for initialization
		public void Start ()
        {
        }


		public void Update ()
        {
            // just convert current touch position into ray
            if (Input.touchCount > 0) {
                Vector2f touchPos = Input.touches[0].position;
                Vector3f touchPos3 = new Vector3f(touchPos.x, touchPos.y, 0);
                CurrentWorldRay = ((Camera)context.ActiveCamera).ScreenPointToRay(touchPos3);
                CurrentUIRay = ((Camera)context.OrthoUICamera).ScreenPointToRay(touchPos3);
            }
            if (Input.touchCount > 1) {
                Vector2f touchPos = Input.touches[1].position;
                Vector3f touchPos3 = new Vector3f(touchPos.x, touchPos.y, 0);
                secondWorldRayValid = true;
                secondWorldRay = ((Camera)context.ActiveCamera).ScreenPointToRay(touchPos3);
            } else {
                secondWorldRayValid = false;
            }
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