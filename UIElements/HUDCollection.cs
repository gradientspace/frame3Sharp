using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace f3
{

    //
    // Just a grouping of child UIElements, theoretically can be used
    // to transform the set but that is not the intention. Just useful
    // for organizational purposes (eg can hide/show all at once)
    //
    public class HUDCollection : SceneUIElement
    {
        public List<SceneUIElement> Children { get; set; }

        GameObject gameObject;
        //SceneUIElement pCapturing;
        //SceneUIElement pHovering;

        public HUDCollection()
        {
            Children = new List<SceneUIElement>();
        }

        public virtual void Create()
        {
            gameObject = new GameObject(UniqueNames.GetNext("HUDCollection"));
        }

        public virtual void AddChild(SceneUIElement ui)
        {
            if (!Children.Contains(ui)) {
                Children.Add(ui);
                ui.RootGameObject.transform.SetParent(gameObject.transform, true);
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
                ui.RootGameObject.transform.SetParent(null);

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
         *  SceneUIElement impl
         */

        public bool EnableHover
        {
            get {
                foreach (var ui in Children)
                    if (ui.EnableHover)
                        return true;
                return false;
            }
        }

        public GameObject RootGameObject
        {
            get {
                return gameObject;
            }
        }

        public void Disconnect()
        {
            foreach (var ui in Children)
                ui.Disconnect();
        }

        public virtual bool IsVisible {
            get {
                // this is a bit meaningless...
                return UnityUtil.IsVisible(RootGameObject);
            }
            set {
                UnityUtil.SetVisible(RootGameObject, value);
                foreach (var ui in Children)
                    ui.IsVisible = value;
            }
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


        public bool FindRayIntersection(Ray ray, out UIRayHit hit)
        {
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



        public bool FindHoverRayIntersection(Ray ray, out UIRayHit hit)
        {
            return HUDUtil.FindNearestHoverRayIntersection(Children, ray, out hit);
        }


        // [RMS] I don't think we ever actually call these functions, do we?? 
        //   hover is sent directly to child that is ray-hit via above, right?

        public void UpdateHover(Ray ray, UIRayHit hit)
        {
            throw new InvalidOperationException("HUDCollection.UpdateHover: how is this being called?");
        }

        public void EndHover(Ray ray)
        {
            throw new InvalidOperationException("HUDCollection.EndHover: how is this being called?");
        }



    }
}
