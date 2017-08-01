using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{

    public interface IChildren<T> : IEnumerable<T>
    {
        void Add(T child);
        void Remove(T child);
    }


    /// <summary>
    /// HUDChildren stores/manipulates a set of child SceneUIElements. The idea is that
    /// instead of storing the List in a widget itself (eg like a HUDPanel), this class
    /// provides a more structured component, that can be passed to other objects. The
    /// parent listens to the OnChildAdded/OnChildRemoved events to find out about 
    /// modifications to the list.
    /// 
    /// This allows us to, for example, pass the HUDChildren to layout classes like
    /// HUDElementLayout. Then we can add widgets to the Layout, which will then
    /// add to the HUDChildren, and the parent widget hear about the changes via the events.
    /// </summary>
    public class HUDChildren : IChildren<SceneUIElement>
    {
        SceneUIParent Parent;
        List<SceneUIElement> Children;

        public Action<SceneUIElement, bool> OnChildAdded;
        public Action<SceneUIElement> OnChildRemoved;


        public HUDChildren(SceneUIParent parent)
        {
            this.Parent = parent;
            this.Children = new List<SceneUIElement>();
        }


        public virtual void Add(SceneUIElement ui)
        {
            Add(ui, true);
        }

        public virtual void Add(SceneUIElement ui, bool bKeepWorldPosition)
        {
            if ( ! Children.Contains(ui) ) {
                Children.Add(ui);
                ui.Parent = Parent;
                if (OnChildAdded != null)
                    OnChildAdded(ui, bKeepWorldPosition);
            }
        }
        public virtual void Add(IEnumerable<SceneUIElement> v, bool bKeepWorldPosition = true)
        {
            foreach (SceneUIElement ui in v)
                Add(ui, bKeepWorldPosition);
        }



        public virtual void Remove(SceneUIElement ui)
        {
            if (Children.Contains(ui)) {
                Children.Remove(ui);
                ui.Parent = null;
                ui.RootGameObject.SetParent(null, true);

                if (OnChildRemoved != null)
                    OnChildRemoved(ui);

                // [RMS] should re-parent to cockpit/scene we are part of? currently no reference to do that...
                //so.RootGameObject.transform.SetParent(parentScene.RootGameObject.transform, true);
            }
        }

        public virtual void RemoveAll()
        {
            while (Children.Count > 0)
                Remove(Children[0]);
        }




        public IEnumerator<SceneUIElement> GetEnumerator() {
            return Children.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return Children.GetEnumerator();
        }

    }
}
