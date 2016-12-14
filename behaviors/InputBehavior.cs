using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{
    public enum CaptureState
    {
        Begin, Continue, End, Ignore
    }
    public enum CaptureSide
    {
        Left = 0, Right = 1, Both = 2, Any = 3
    }

    // [TODO] should this be a struct?
    public struct CaptureData
    {
        public CaptureSide which;
        public object custom_data;
    }



    public enum CaptureRequestType
    {
        Begin, Interrupt, Ignore
    }
    public class CaptureRequest
    {
        public InputBehavior element;
        public CaptureRequestType type;
        public CaptureSide side;

        public CaptureRequest(CaptureRequestType t, InputBehavior e, CaptureSide which) {
            this.type = t;
            this.element = e;
            this.side = which;
        }

        public static CaptureRequest Begin(InputBehavior e, CaptureSide which = CaptureSide.Any) {
            return new CaptureRequest(CaptureRequestType.Begin, e, which);
        }
        public static CaptureRequest Interrupt(InputBehavior e, CaptureSide which = CaptureSide.Any) {
            return new CaptureRequest(CaptureRequestType.Interrupt, e, which);
        }

        static public readonly CaptureRequest Ignore = new CaptureRequest(CaptureRequestType.Ignore, null, CaptureSide.Any);
    }




    // TODO: possibly should refactor into separate Begin/other object,
    //   as the way we are using is that BeginCapture() returns complex
    //   object but Update/End just return the constant ones...
    public class Capture
    {
        public CaptureState state;
        public InputBehavior element;
        public CaptureData data;

        public static Capture Begin(InputBehavior e, CaptureSide which = CaptureSide.Any, object custom_data = null)
        {
            return new Capture(CaptureState.Begin, e, which, custom_data);
        }

        static public readonly Capture Continue = new Capture(CaptureState.Continue, null);
        static public readonly Capture End = new Capture(CaptureState.End, null);
        static public readonly Capture Ignore = new Capture(CaptureState.Ignore, null);


        Capture(CaptureState state, InputBehavior e, CaptureSide which = CaptureSide.Any, object custom_data = null)
        {
            this.state = state;
            this.element = e;

            this.data = new CaptureData();
            this.data.which = which;
            this.data.custom_data = custom_data;
        }
    }




    public interface InputBehavior
    {
        string CaptureIdentifier { get; }
        int Priority { get; }
        InputDevice SupportedDevices { get; }

        CaptureRequest WantsCapture(InputState input);
        Capture BeginCapture(InputState input, CaptureSide eSide);
        Capture UpdateCapture(InputState input, CaptureData data);
        Capture ForceEndCapture(InputState input, CaptureData data);

        bool EnableHover { get; }
        void UpdateHover(InputState input);
        void EndHover(InputState input);
    }



    public abstract class StandardInputBehavior : InputBehavior
    {
        //public abstract string CaptureIdentifier { get; }

        public virtual string CaptureIdentifier { get { return this.GetType().ToString(); } }

        public int Priority { get; set; }
        public abstract InputDevice SupportedDevices { get; }

        public abstract CaptureRequest WantsCapture(InputState input);
        public abstract Capture BeginCapture(InputState input, CaptureSide eSide);
        public abstract Capture UpdateCapture(InputState input, CaptureData data);
        public abstract Capture ForceEndCapture(InputState input, CaptureData data);

        // default hover behavior is to do nothing
        public virtual bool EnableHover {
            get { return false; }
        }
        public virtual void UpdateHover(InputState input)
        {
        }
        public virtual void EndHover(InputState input)
        {
        }
    }

}
