using System;
using System.Collections.Generic;

namespace f3
{

    /// <summary>
    /// ActionSet is just a collection of named Actions that can be called via Run()
    /// </summary>
    public class ActionSet
    {
        Dictionary<string, Action> Actions = new Dictionary<string, Action>();
        Dictionary<string, ActionWithData> DataActions = new Dictionary<string, ActionWithData>();

        public struct ActionWithData
        {
            public Action<object> F;
            public object D;
        }


        public ActionSet()
        {

        }


        public virtual void RegisterAction(string name, Action action)
        {
            if (Actions.ContainsKey(name) || DataActions.ContainsKey(name) )
                throw new Exception("TriggerSet.RegisterAction: handler " + name + " already exists!");
            Actions.Add(name, action);
        }

        public virtual void RegisterAction(string name, Action<object> action, object data)
        {
            if (Actions.ContainsKey(name) || DataActions.ContainsKey(name) )
                throw new Exception("TriggerSet.RegisterAction: handler " + name + " already exists!");
            DataActions.Add(name, new ActionWithData() { F = action, D = data } );
        }

        public virtual bool RemoveAction(string name)
        {
            if (Actions.ContainsKey(name)) {
                Actions.Remove(name);
                return true;
            } else if ( DataActions.ContainsKey(name) ) {
                DataActions.Remove(name);
                return true;
            }
            return false;
        }


        public virtual void Run()
        {
            foreach (Action a in Actions.Values)
                a();
            foreach ( var a in DataActions)
                a.Value.F(a.Value.D);
        }


    }
}
