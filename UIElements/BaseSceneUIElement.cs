using System;
using g3;

namespace f3
{
    /// <summary>
    /// Base implementation for SceneUIElement
    /// </summary>
    public class BaseSceneUIElement : SceneUIElement
    {
        fGameObject rootGO = null;
        SceneUIParent parent;

        public BaseSceneUIElement(string name)
        {
            rootGO = GameObjectFactory.CreateParentGO(name);
        }

        public virtual fGameObject RootGameObject { get { return rootGO; } }

        public virtual string Name {
            get { return RootGameObject.GetName(); }
            set { RootGameObject.SetName(value); }
        }

        public virtual SceneUIParent Parent {
            get { return parent; }
            set { parent = value; }
        }

        public virtual void Disconnect()
        {
            FUtil.SafeSendEvent(OnDisconnected, this, EventArgs.Empty);
        }
        public event EventHandler OnDisconnected;

        public virtual bool IsVisible { get { return true; } set { } }
        public virtual bool IsInteractive { get { return false; } set { } }
        public virtual void SetLayer(int nLayer) { rootGO.SetLayer(nLayer, true); }

        // called on per-frame Update()
        public virtual void PreRender() { }

        public virtual bool FindRayIntersection(Ray3f ray, out UIRayHit hit) { hit = null; return false; }
        public virtual bool FindHoverRayIntersection(Ray3f ray, out UIRayHit hit) { hit = null; return false; }
        public virtual bool WantsCapture(InputEvent e) { return false; }
        public virtual bool BeginCapture(InputEvent e) { return false; }
        public virtual bool UpdateCapture(InputEvent e) { return false; }
        public virtual bool EndCapture(InputEvent e) { return false; }
        public virtual bool EnableHover { get { return false; } }
        public virtual void UpdateHover(Ray3f ray, UIRayHit hit) { }
        public virtual void EndHover(Ray3f ray) { }


    }
}
