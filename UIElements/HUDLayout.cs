using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using g3;

namespace f3
{

    /// <summary>
    /// Base interface for anything that can compute a Layout
    /// (maybe this is useless??)
    /// </summary>
    public interface ILayout
    {
        void RecomputeLayout();

        void AddLayoutItem(SceneUIElement element);
        bool RemoveLayoutItem(SceneUIElement element);
    }



    /// <summary>
    /// Standard base class for 2D layouts. Stores a set of SceneUIElement objects.
    /// </summary>
    public abstract class HUDLayout : ILayout
    {
        // force immediate layout recomputation
        public abstract void RecomputeLayout();


        protected List<SceneUIElement> LayoutItems = new List<SceneUIElement>();
        public ReadOnlyCollection<SceneUIElement> Items
        {
            get { return LayoutItems.AsReadOnly(); }
        }


        public virtual void AddLayoutItem(SceneUIElement element)
        {
            if (LayoutItems.Contains(element))
                throw new Exception("HUDLayout.AddLayoutItem: element " + element.Name + " already in layout");

            LayoutItems.Add(element);
        }

        public virtual bool RemoveLayoutItem(SceneUIElement element)
        {
            if ( LayoutItems.Contains(element) ) {
                LayoutItems.Remove(element);
                return true;
            }
            return false;
        }


    }




}
