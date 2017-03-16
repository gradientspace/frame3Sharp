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


        public virtual void Run()
        {
            foreach (Action a in Actions)
                a();
            foreach ( var a in DataActions)
                a.F(a.D);

            foreach (Action a in NamedActions.Values)
                a();
            foreach ( var a in NamedDataActions)
                a.Value.F(a.Value.D);
        }


    }
}
