using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace f3
{

    //
    // Just a grouping of child UIElements, theoretically can be used
    // to transform the set but that is not the intention. Just useful
    // for organizational purposes (eg can hide/show all at once)
    //
    public class HUDCollection : SceneUIParent, SceneUIElement
    {
        public List<SceneUIElement> Children { get; set; }

        fGameObject gameObject;
        SceneUIParent parent;
        //SceneUIElement pCapturing;
        //SceneUIElement pHovering;
        bool is_interactive = true;

        public HUDCollection()
        {
            Children = new List<SceneUIElement>();
        }

        public virtual void Create()
        {
            gameObject = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDCollection"));
        }

        public virtual void AddChild(SceneUIElement ui)
        {
            if (!Children.Contains(ui)) {
                Children.Add(ui);
                ui.Parent = this;
                ui.RootGameObject.SetParent(gameObject, true);
            }
        }
        public virtual void AddChildren(IEnumerable<SceneUIElement> v)
        {
            foreach (SceneUIElement ui in v)
                AddChild(ui);
        }

        public virtual void RemoveChild(SceneUIElement ui)
        {
            if (Children.Contains(ui)) {
                Children.Remove(ui);
                ui.Parent = null;
                ui.RootGameObject.SetParent(null, true);

                // [RMS] should re-parent to cockpit/scene we are part of? currently no reference to do that...
                //so.RootGameObject.transform.SetParent(parentScene.RootGameObject.transform, true);
            }
        }

        public virtual void RemoveAllChildren()
        {
            while (Children.Count > 0)
                RemoveChild(Children[0]);
        }



        /*
         * SceneUIParent impl 
         */
        public virtual FContext Context {
            get { return Parent.Context; }
        }


        /*
         *  SceneUIElement impl
         */

        public fGameObject RootGameObject
        {
            get {
                return gameObject;
            }
        }

		public virtual SceneUIParent Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public virtual string Name
        {
            get {
                return RootGameObject.GetName();
            }
            set {
                RootGameObject.SetName(value);
            }
        }


        public void Disconnect()
        {
            foreach (var ui in Children)
                ui.Disconnect();
            FUtil.SafeSendEvent(OnDisconnected, this, EventArgs.Empty);
        }
        public event EventHandler OnDisconnected;


        public virtual bool IsVisible {
            get {
                // this is a bit meaningless...
                return RootGameObject.IsVisible();
            }
            set {
                RootGameObject.SetVisible(value);
                foreach (var ui in Children)
                    ui.IsVisible = value;
            }
        }

        public virtual bool IsInteractive {
            get { return is_interactive; }
            set { is_interactive = value; }
        }

        public void SetLayer(int nLayer)
        {
            foreach (var ui in Children)
                ui.SetLayer(nLayer);
        }

        // called on per-frame Update()
        public void PreRender()
        {
            foreach (var ui in Children)
                ui.PreRender();
        }


        public bool FindRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            if (is_interactive == false) {
                hit = null;
                return false;
            }
            return HUDUtil.FindNearestRayIntersection(Children, ray, out hit);
        }

        virtual public bool WantsCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDCollection.WantsCapture : how is this being called?");
        }
        virtual public bool BeginCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDCollection.BeginCapture: how is this being called?");
        }
        virtual public bool UpdateCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDCollection.UpdateCapture: how is this being called?");
        }
        virtual public bool EndCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDCollection.EndCapture: how is this being called?");
        }





        public bool EnableHover
        {
            get {
                if (is_interactive == false)
                    return false;
                foreach (var ui in Children)
                    if (ui.EnableHover)
                        return true;
                return false;
            }
        }


        public bool FindHoverRayIntersection(Ray3f ray, out UIRayHit hit)
        {
            if (is_interactive == false) {
                hit = null;
                return false;
            }
            return HUDUtil.FindNearestHoverRayIntersection(Children, ray, out hit);
        }


        // [RMS] I don't think we ever actually call these functions, do we?? 
        //   hover is sent directly to child that is ray-hit via above, right?

        public void UpdateHover(Ray3f ray, UIRayHit hit)
        {
            throw new InvalidOperationException("HUDCollection.UpdateHover: how is this being called?");
        }

        public void EndHover(Ray3f ray)
        {
            throw new InvalidOperationException("HUDCollection.EndHover: how is this being called?");
        }

    }
}
