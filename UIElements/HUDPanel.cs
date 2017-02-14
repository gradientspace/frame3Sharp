using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using g3;

namespace f3
{

    //
    // panel is like a 2D window, kind of...
    //
    public class HUDPanel : HUDStandardItem, SceneUIParent, IBoxModelElement
    {
        public List<SceneUIElement> Children { get; set; }

        public float Width { get; set; }
        public float Height { get; set; }


        fGameObject gameObject;
        //SceneUIElement pCapturing;
        //SceneUIElement pHovering;

        public HUDPanel()
        {
            Children = new List<SceneUIElement>();
        }



        public virtual void Create()
        {
            gameObject = GameObjectFactory.CreateParentGO(UniqueNames.GetNext("HUDPanel"));
        }


        // [RMS] management of Panel children. Currently we do not use Panel
        //   directly, so these are not publicly accessible. I don't entirely like this.
        //   However, C# does not allow us to "hide" a public member in a subclass,
        //   which means that Panel implementations would directly expose these, when
        //   in most cases they should not be exposed...

        protected virtual void AddChild(SceneUIElement ui)
        {
            if (!Children.Contains(ui)) {
                Children.Add(ui);
                ui.Parent = this;
                gameObject.AddChild(ui.RootGameObject, true);
            }
        }
        protected virtual void AddChildren(IEnumerable<SceneUIElement> v)
        {
            foreach (SceneUIElement ui in v)
                AddChild(ui);
        }

        protected virtual void RemoveChild(SceneUIElement ui)
        {
            if (Children.Contains(ui)) {
                Children.Remove(ui);
                ui.Parent = null;
                ui.RootGameObject.transform.SetParent(null);

                // [RMS] should re-parent to cockpit/scene we are part of? currently no reference to do that...
                //so.RootGameObject.transform.SetParent(parentScene.RootGameObject.transform, true);
            }
        }

        protected virtual void RemoveAllChildren()
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

        public override GameObject RootGameObject
        {
            get {
                return (gameObject == null) ? null : gameObject;
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();
            foreach (var ui in Children)
                ui.Disconnect();
        }

        override public bool IsVisible {
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


        override public void SetLayer(int nLayer)
        {
            base.SetLayer(nLayer);
            foreach (var ui in Children)
                ui.SetLayer(nLayer);
        }

        // called on per-frame Update()
        override public void PreRender()
        {
            base.PreRender();
            foreach (var ui in Children)
                ui.PreRender();
        }


        override public bool FindRayIntersection(Ray ray, out UIRayHit hit)
        {
            return HUDUtil.FindNearestRayIntersection(Children, ray, out hit);
        }

        override public bool WantsCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.WantsCapture : how is this being called?");
        }
        override public bool BeginCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.BeginCapture: how is this being called?");
        }
        override public bool UpdateCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.UpdateCapture: how is this being called?");
        }
        override public bool EndCapture(InputEvent e)
        {
            throw new InvalidOperationException("HUDPanel.EndCapture: how is this being called?");
        }



        public override bool FindHoverRayIntersection(Ray ray, out UIRayHit hit)
        {
            return HUDUtil.FindNearestHoverRayIntersection(Children, ray, out hit);
        }


        // [RMS] I don't think we ever actually call these functions, do we?? 
        //   hover is sent directly to child that is ray-hit via above, right?

        public override bool EnableHover
        {
            get {
                foreach (var ui in Children)
                    if (ui.EnableHover)
                        return true;
                return false;
            }
        }

        public override void UpdateHover(Ray ray, UIRayHit hit)
        {
            throw new InvalidOperationException("HUDPanel.UpdateHover: how is this being called?");
        }

        public override void EndHover(Ray ray)
        {
            throw new InvalidOperationException("HUDPanel.EndHover: how is this being called?");
        }




       #region IBoxModelElement implementation


        public Vector2f Size2D {
            get { return new Vector2f(Width, Height); }
        }

        public AxisAlignedBox2f Bounds2D { 
            get { return new AxisAlignedBox2f(Vector2f.Zero, Width/2, Height/2); }
        }

        #endregion


    }
}
