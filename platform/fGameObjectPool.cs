using System;
using System.Collections.Generic;
using g3;

namespace f3
{
    /// <summary>
    /// Pool of re-usable fGameObject instances, allocated by factory function you must provide to constructor.
    /// Unused GOs are automatically parented to per-instance hidden parent GO, to avoid cluttering scene.
    /// When a GO is freed, it is hidden.
    /// When a GO is allocated from pool, it is set to have no parent, visible, and transform is cleared.
    /// if FreeF is set, this is called when a GO is freed
    /// If ReinitializeF is set, this is also called on re-used GOs (*not* called on newly-allocated GOs as
    ///    you can do that in your FactoryF)
    /// </summary>
    public class fGameObjectPool<T> where T : fGameObject
    {
        public Func<T> AllocatorFactoryF;
        public Action<T> FreeF;
        public Action<T> ReinitializeF;

        fGameObject pool_parent;

        List<T> Allocated;
        List<T> Free;

        public fGameObjectPool(Func<T> factoryF)
        {
            Allocated = new List<T>();
            Free = new List<T>();

            pool_parent = GameObjectFactory.CreateParentGO("pool_parent");

            AllocatorFactoryF = factoryF;
        }


        public void Destroy()
        {
            foreach (var go in Allocated)
                go.Destroy();
            Allocated.Clear();
            Free.Clear();
            pool_parent.Destroy();
        }


        public T Allocate()
        {
            if (Free.Count > 0) {
                T go = Free[Free.Count - 1];
                Free.RemoveAt(Free.Count - 1);
                go.SetParent(null);
                go.SetVisible(true);
                go.SetLocalFrame(Frame3f.Identity);
                go.SetLocalScale(1.0f);
                if (ReinitializeF != null)
                    ReinitializeF(go);
                return go;
            } else {
                T go = AllocatorFactoryF();
                Allocated.Add(go);
                return go;
            }
        }


        public void FreeAll()
        {
            foreach (T go in Allocated) {
                if (go.IsVisible()) {
                    FreeF(go);
                    go.SetVisible(false);
                    go.SetParent(pool_parent);
                }
            }
            Free.Clear();
            Free.AddRange(Allocated);
        }

    }
}
