using System;
using System.Collections.Generic;

namespace Farbase
{
    public interface Property
    {
        object GetValue();
        void SetValue(object o);
        object At(int index);
    }

    public class Property<T> : Property
    {
        protected T value;

        public Property(T val)
        {
            value = val;
        }

        object Property.GetValue() { return GetValue(); }
        private T GetValue() { return value; }

        void Property.SetValue(object o)
        {
            SetValue((T)o);
        }
        public void SetValue(T val)
        {
            if (!value.Equals(val))
            {
                //report to subscribers?
                //since we know the value is new and not just the same one
                //set again.
                value = val;
            }
        }

        //this is not very beautiful
        //but it makes us able to use indexed properties in a nice way.
        //could probably make this just return its normal value?
        //although we might actually *want* to crash if you try to index
        //a non-indexable...
        public virtual object At(int index)
        {
            throw new NotSupportedException();
        }
    }

    public class ListProperty<T> : Property<List<T>> 
    {
        public ListProperty(List<T> val) : base(val) { }

        public override object At(int index)
        {
            if (index >= value.Count) return null;
            return value[index];
        }
    }
}