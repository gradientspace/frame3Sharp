using System;
using System.Collections.Generic;

namespace f3
{

    /// <summary>
    /// ActionSet is just a collection of Actions that can be called via Run()
    /// </summary>
    public class ActionSet
    {
        List<Action> Actions = new List<Action>();
        List<ActionWithData> DataActions = new List<ActionWithData>();

        Dictionary<string, Action> NamedActions = new Dictionary<string, Action>();
        Dictionary<string, ActionWithData> NamedDataActions = new Dictionary<string, ActionWithData>();

        public struct ActionWithData
        {
            public Action<object> F;
            public object D;
        }


        public ActionSet()
        {

        }


        public void Clear()
        {
            Actions.Clear();
            DataActions.Clear();
            NamedActions.Clear();
            NamedDataActions.Clear();
        }


        public virtual void RegisterAction(Action action)
        {
            Actions.Add(action);
        }

        public virtual void RegisterAction(Action<object> action, object data)
        {
            DataActions.Add(new ActionWithData() { F = action, D = data } );
        }

        public virtual void RegisterAction(string name, Action action)
        {
            if (NamedActions.ContainsKey(name) || NamedDataActions.ContainsKey(name) )
                throw new Exception("TriggerSet.RegisterAction: handler " + name + " already exists!");
            NamedActions.Add(name, action);
        }

        public virtual void RegisterAction(string name, Action<object> action, object data)
        {
            if (NamedActions.ContainsKey(name) || NamedDataActions.ContainsKey(name) )
                throw new Exception("TriggerSet.RegisterAction: handler " + name + " already exists!");
            NamedDataActions.Add(name, new ActionWithData() { F = action, D = data } );
        }

        public virtual bool RemoveAction(string name)
        {
            if (NamedActions.ContainsKey(name)) {
                NamedActions.Remove(name);
                return true;
            } else if ( NamedDataActions.ContainsKey(name) ) {
                NamedDataActions.Remove(name);
                return true;
            }
            return false;
        }



        /// <summary>
        /// This method returns an Action that can be executed to run the
        /// current set of actions. The reason for this function is that if
        /// you are using ActionSet in a context where you might Clear() it,
        /// ie to do single-shot actions, if you call Run() and then Clear(),
        /// then if any registered Actions are registering other Actions, they
        /// would be immediately discarded. This prevents useful idioms like
        /// chaining a sequence of Actions.
        /// So usage would be:
        ///   Action saveF = set.GetRunnable();    set.Clear();    saveF();
        /// </summary>
        public Action GetRunnable()
        {
            Action[] copyActions = Actions.ToArray();
            ActionWithData[] copyDataActions = DataActions.ToArray();
            Action[] copyNamedActions = new Action[NamedActions.Count];
            NamedActions.Values.CopyTo(copyNamedActions, 0);
            ActionWithData[] copyNamedDataActions = new ActionWithData[NamedDataActions.Count];
            NamedDataActions.Values.CopyTo(copyNamedDataActions, 0);

            Action runnable = () => {
                for (int i = 0; i < copyActions.Length; ++i)
                    copyActions[i]();
                for (int i = 0; i < copyDataActions.Length; ++i)
                    copyDataActions[i].F(copyDataActions[i].D);
                for (int i = 0; i < copyNamedActions.Length; ++i)
                    copyNamedActions[i]();
                for (int i = 0; i < copyNamedDataActions.Length; ++i)
                    copyNamedDataActions[i].F(copyNamedDataActions[i].D);
            };
            return runnable;
        }




        public virtual void Run()
        {
            // We have to make copies in case an Action adds other Actions
            // (this would break the iterators). Also minmizes ordering effects.
            Action F = GetRunnable();
            F();
        }


    }
}
