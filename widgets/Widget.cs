using System;
using UnityEngine;

namespace f3
{
	public interface Widget
	{
		bool BeginCapture(ITransformable target, Ray worldRay, UIRayHit hit);
		bool UpdateCapture(ITransformable target, Ray worldRay);
        bool EndCapture(ITransformable target);
	}


    // convenience impl
    public abstract class Standard3DWidget : Widget
    {
        public Material StandardMaterial { get; set; }
        public Material HoverMaterial { get; set; }
        public GameObject RootGameObject { get; set; }

        public Standard3DWidget()
        {

        }

        public abstract bool BeginCapture(ITransformable target, Ray worldRay, UIRayHit hit);
        public abstract bool UpdateCapture(ITransformable target, Ray worldRay);
        public abstract bool EndCapture(ITransformable target);
    }
}

