using System;
using g3;

namespace f3
{
	public interface Widget
	{
        void Disconnect();

		bool BeginCapture(ITransformable target, Ray3f worldRay, UIRayHit hit);
		bool UpdateCapture(ITransformable target, Ray3f worldRay);
        bool EndCapture(ITransformable target);
	}


    // convenience impl
    public abstract class Standard3DWidget : Widget
    {
        public fMaterial StandardMaterial { get; set; }
        public fMaterial HoverMaterial { get; set; }
        public fGameObject RootGameObject { get; set; }

        public Standard3DWidget()
        {

        }

        public abstract void Disconnect();
        public abstract bool BeginCapture(ITransformable target, Ray3f worldRay, UIRayHit hit);
        public abstract bool UpdateCapture(ITransformable target, Ray3f worldRay);
        public abstract bool EndCapture(ITransformable target);
    }
}

