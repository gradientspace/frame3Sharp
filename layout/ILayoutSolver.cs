using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using g3;

namespace f3
{

    /// <summary>
    /// Base interface for anything that can compute a layout
    /// </summary>
    public interface ILayoutSolver
    {
        void RecomputeLayout();

        void AddLayoutItem(SceneUIElement element);
        bool RemoveLayoutItem(SceneUIElement element);
    }



    /// <summary>
    /// Standard base class for layout solver. Stores a set of SceneUIElement objects.
    /// You still need to implement RecomputeLayout().
    /// </summary>
    public abstract class BaseLayoutSolver : ILayoutSolver
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
