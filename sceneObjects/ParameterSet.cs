using System;
using System.Collections.Generic;

namespace f3
{

    public delegate void ParameterModifiedEvent(ParameterSet pset, string sParamName);

    public abstract class AnyParameter
    {
        public string name;
        public bool isAlias = false;        // if true, this parameter is an alias for another
                                            // parameter, which means things like we don't also Reset() it, etc

        abstract public string TypeName();
    }

    public abstract class IParameter<T> : AnyParameter
    {
        public Func<T> getValue;
        public Action<T> setValue;
        public T defaultValue;

        virtual public void Reset() {
            setValue(defaultValue);
        }
    }

    public class FloatParameter : IParameter<float>
    {
        public FloatParameter()
        {
            name = "float_parameter";
            defaultValue = 0.0f;
        }
        override public string TypeName() { return "float"; }
    }

    public class IntParameter : IParameter<int>
    {
        public IntParameter()
        {
            name = "int_parameter";
            defaultValue = 0;
        }
        override public string TypeName() { return "int"; }
    }

    public class BoolParameter : IParameter<bool>
    {
        public BoolParameter()
        {
            name = "bool_parameter";
            defaultValue = false;
        }
        override public string TypeName() { return "bool"; }
    }



    public class ParameterSet
    {
        List<AnyParameter> vParameters;

        public ParameterSet()
        {
            vParameters = new List<AnyParameter>();
        }

        public virtual List<AnyParameter> Parameters {
            get { return vParameters;  }
        }

        public ParameterModifiedEvent OnParameterModified;
        void on_parameter_modified(string sName) {
            var tmp = OnParameterModified;
            if (tmp != null) tmp(this, sName);
        }


        public virtual bool Register(string sName, Func<float> getValue, Action<float> setValue, float defaultValue, bool isAlias = false) {
            if (FindByName(sName) != null)
                return false;
            vParameters.Add(new FloatParameter() 
                { name = sName, getValue = getValue, setValue = setValue, defaultValue = defaultValue, isAlias = isAlias });
            return true;
        }
        public virtual bool Register(string sName, Func<int> getValue, Action<int> setValue, int defaultValue, bool isAlias = false) {
            if (FindByName(sName) != null)
                return false;
            vParameters.Add(new IntParameter() 
                { name = sName, getValue = getValue, setValue = setValue, defaultValue = defaultValue, isAlias = isAlias });
            return true;
        }
        public virtual bool Register(string sName, Func<bool> getValue, Action<bool> setValue, bool defaultValue, bool isAlias = false) {
            if (FindByName(sName) != null)
                return false;
            vParameters.Add(new BoolParameter() 
                { name = sName, getValue = getValue, setValue = setValue, defaultValue = defaultValue, isAlias = isAlias });
            return true;
        }


        public AnyParameter FindByName(string sName) {
            return vParameters.Find((s) => s.name == sName);
        }
        public bool HasParameter(string sName) {
            return FindByName(sName) != null;
        }


        public virtual float GetValueFloat(string sName) {
            AnyParameter p = FindByName(sName);
            if (p == null )
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " does not exist in this set");
            if ((p is FloatParameter) == false)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " is of type + " + p.TypeName());
            return (p as FloatParameter).getValue();
        }
        public virtual int GetValueInt(string sName) {
            AnyParameter p = FindByName(sName);
            if (p == null)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " does not exist in this set");
            if ((p is IntParameter) == false)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " is of type + " + p.TypeName());
            return (p as IntParameter).getValue();
        }
        public virtual bool GetValueBool(string sName){
            AnyParameter p = FindByName(sName);
            if (p == null)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " does not exist in this set");
            if ((p is BoolParameter) == false)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " is of type + " + p.TypeName());
            return (p as BoolParameter).getValue();
        }

        public virtual T GetValue<T>(string sName)
        {
            AnyParameter p = FindByName(sName);
            if (p == null)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " does not exist in this set");
            if ((p is IParameter<T>) == false)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " is of type + " + p.TypeName());
            return (p as IParameter<T>).getValue();
        }



        public virtual void SetValue<T>(string sName, T value)
        {
            AnyParameter p = FindByName(sName);
            if (p == null || (p is IParameter<T>) == false)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " is of type + " + p.TypeName());
            (p as IParameter<T>).setValue(value);
            on_parameter_modified(sName);
        }



    }
}
