using System;
using g3;

namespace f3
{
    /// <summary>
    /// This is a thread-safe fLineGameObject wrapper. You can construct it in a
    /// background thread, the line will not be constructed until we are running in
    /// the main thread (via FContext NextFrameAction). Similarly if you change
    /// parameter values, they will be applied in the main thread.
    /// 
    /// (Currently) the parameters are implemented as Func<type>, rather than 
    /// explicit values. This means you can hook it up to something else and it
    /// will auto-update each frame.
    /// 
    /// This is all relatively heavy, you should not be using this for most lines.
    /// However it is very useful for debug visualizations, and/or tacking on a line
    /// to an existing thing.
    /// </summary>
    public class AutoLineGameObject
    {
        public fLineGameObject LineGO;

        // [TODO] these should be locked!!
        public Func<string> NameF = () => { return "AutoLine"; };
        public Func<Colorf> ColorF = () => { return Colorf.Red; };
        public Func<Vector3f> StartF = () => { return Vector3f.Zero; };
        public Func<Vector3f> EndF = () => { return Vector3f.One; };
        public Func<float> LineWidthF = () => { return 1.0f; };

        public bool DeleteNextFrame = false;
        public Func<bool> DeleteConditionF = () => { return false; };

        public Action<fLineGameObject> OnCreateF = null;


        public AutoLineGameObject(FContext context, fGameObject parentGO = null)
        {
            context.RegisterNextFrameAction(() => {
                this.InitOnMainThread(parentGO);
            });
        }


        public AutoLineGameObject(FContext context, Vector3d p0, Vector3d p1, Colorf color, float lineWidth = 1.0f, string Name = "AutoLine", fGameObject parentGO = null)
        {
            this.NameF = () => { return Name; };
            this.ColorF = () => { return color; };
            this.StartF = () => { return (Vector3f)p0; };
            this.EndF = () => { return (Vector3f)p1; };
            this.LineWidthF = () => { return lineWidth; };

            context.RegisterNextFrameAction(() => {
                this.InitOnMainThread(parentGO);
            });
        }


        public AutoLineGameObject(FContext context, Func<Vector3f> startF, Func<Vector3f> endF, Colorf color, float lineWidth = 1.0f, string Name = "AutoLine", fGameObject parentGO = null)
        {
            this.NameF = () => { return Name; };
            this.ColorF = () => { return color; };
            this.StartF = startF;
            this.EndF = endF;
            this.LineWidthF = () => { return lineWidth; };

            context.RegisterNextFrameAction(() => {
                this.InitOnMainThread(parentGO);
            });
        }


        void InitOnMainThread(fGameObject parentGO)
        {
            LineGO = GameObjectFactory.CreateLineGO(NameF(), ColorF(), LineWidthF(), LineWidthType.World);
            LineGO.SetStart(StartF());
            LineGO.SetEnd(EndF());
            if (parentGO != null)
                parentGO.AddChild(LineGO, false);

            LineGO.GetComponent<PreRenderBehavior>().AddAction(() => { Update(); });

            if (OnCreateF != null)
                OnCreateF(LineGO);
        }


        void Update()
        {
            LineGO.SetName(NameF());
            LineGO.SetColor(ColorF());
            LineGO.SetStart(StartF());
            LineGO.SetEnd(EndF());
            LineGO.SetLineWidth(LineWidthF());

            if (DeleteConditionF() == true || DeleteNextFrame == true)
                LineGO.Destroy();
        }


        public bool IsCreated {
            get { return LineGO != null; }
        }


        public fGameObject RootGameObject {
            get { return LineGO; }
        }


    }
}
