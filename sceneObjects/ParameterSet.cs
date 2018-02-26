using System;
using System.Collections;
using System.Collections.Generic;
using g3;

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

        abstract public T ClampToRange(T value);
    }


    public class FloatParameter : IParameter<float>
    {
        Interval1d ValidRange;

        public FloatParameter()
        {
            name = "float_parameter";
            defaultValue = 0.0f;
            ValidRange = new Interval1d(float.MinValue, float.MaxValue);
        }
        override public string TypeName() { return "float"; }

        public void SetValidRange(float min, float max) {
            ValidRange = new Interval1d(min, max);
        }

        override public float ClampToRange(float value) {
            return MathUtil.Clamp(value, (float)ValidRange.a, (float)ValidRange.b);
        }
    }


    public class DoubleParameter : IParameter<double>
    {
        Interval1d ValidRange;

        public DoubleParameter()
        {
            name = "double_parameter";
            defaultValue = 0.0;
            ValidRange = new Interval1d(double.MinValue, double.MaxValue);
        }
        override public string TypeName() { return "double"; }

        public void SetValidRange(double min, double max) {
            ValidRange = new Interval1d(min, max);
        }

        override public double ClampToRange(double value) {
            return MathUtil.Clamp(value, ValidRange.a, ValidRange.b);
        }
    }


    public class IntParameter : IParameter<int>
    {
        Interval1i ValidRange;

        public IntParameter()
        {
            name = "int_parameter";
            defaultValue = 0;
            ValidRange = new Interval1i(int.MinValue, int.MaxValue);
        }
        override public string TypeName() { return "int"; }

        public void SetValidRange(int min, int max) {
            ValidRange = new Interval1i(min, max);
        }

        override public int ClampToRange(int value) {
            return MathUtil.Clamp(value, ValidRange.a, ValidRange.b);
        }
    }


    public class BoolParameter : IParameter<bool>
    {
        public BoolParameter()
        {
            name = "bool_parameter";
            defaultValue = false;
        }
        override public string TypeName() { return "bool"; }

        override public bool ClampToRange(bool value) {
            return value;
        }
    }



    public interface IParameterSource
    {
        ParameterSet Parameters { get; }
    }



    public class ParameterSet : IEnumerable<AnyParameter>
    {
        List<AnyParameter> vParameters;

        public ParameterSet()
        {
            vParameters = new List<AnyParameter>();
        }

        public virtual List<AnyParameter> Parameters {
            get { return vParameters;  }
        }
        public IEnumerator<AnyParameter> GetEnumerator() {
            return vParameters.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return vParameters.GetEnumerator();
        }

        public ParameterModifiedEvent OnParameterModified;
        void on_parameter_modified(string sName) {
            var tmp = OnParameterModified;
            if (tmp != null) tmp(this, sName);
        }


        public virtual FloatParameter Register(string sName, Func<float> getValue, Action<float> setValue, float defaultValue, bool isAlias = false) {
            if (FindByName(sName) != null)
                throw new Exception("ParameterSet.Register: parameter named " + sName + " already registered!");
            var param = new FloatParameter() 
                { name = sName, getValue = getValue, setValue = setValue, defaultValue = defaultValue, isAlias = isAlias };
            vParameters.Add(param);
            return param;
        }

        public virtual DoubleParameter Register(string sName, Func<double> getValue, Action<double> setValue, double defaultValue, bool isAlias = false) {
            if (FindByName(sName) != null)
                throw new Exception("ParameterSet.Register: parameter named " + sName + " already registered!");
            var param = new DoubleParameter() 
                { name = sName, getValue = getValue, setValue = setValue, defaultValue = defaultValue, isAlias = isAlias };
            vParameters.Add(param);
            return param;
        }

        public virtual IntParameter Register(string sName, Func<int> getValue, Action<int> setValue, int defaultValue, bool isAlias = false) {
            if (FindByName(sName) != null)
                throw new Exception("ParameterSet.Register: parameter named " + sName + " already registered!");
            var param = new IntParameter() 
                { name = sName, getValue = getValue, setValue = setValue, defaultValue = defaultValue, isAlias = isAlias };
            vParameters.Add(param);
            return param;
        }

        public virtual BoolParameter Register(string sName, Func<bool> getValue, Action<bool> setValue, bool defaultValue, bool isAlias = false) {
            if (FindByName(sName) != null)
                throw new Exception("ParameterSet.Register: parameter named " + sName + " already registered!");
            var param = new BoolParameter() 
                { name = sName, getValue = getValue, setValue = setValue, defaultValue = defaultValue, isAlias = isAlias };
            vParameters.Add(param);
            return param;
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
        public virtual double GetValueDouble(string sName)
        {
            AnyParameter p = FindByName(sName);
            if (p == null)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " does not exist in this set");
            if ((p is DoubleParameter) == false)
                throw new InvalidOperationException("ParameterSet.GetValue - parameter " + sName + " is of type + " + p.TypeName());
            return (p as DoubleParameter).getValue();
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
            IParameter<T> paramT = p as IParameter<T>;
            T clamped_value = paramT.ClampToRange(value);
            paramT.setValue(clamped_value);
            on_parameter_modified(sName);
        }



    }




    /// <summary>
    /// Wrapper for a bool that provides suitable interface for registering with ParameterSet
    /// </summary>
    public class BoolParameterData
    {
        bool bValue;

        public BoolParameterData(bool initial) {
            bValue = initial;
        }

        public bool getValue() { return bValue; }
        public void setValue(bool value) { bValue = value; }
    }

}
