using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.AspNet.Mvc
{
    public class ViewData : DynamicObject
    {
        private Dictionary<object, dynamic> _data;

        public ViewData()
        {
            _data = new Dictionary<object, dynamic>();
        }

        public ViewData(ViewData source)
        {
            _data = new Dictionary<object, dynamic>(source._data);
        }

        public dynamic this[string index]
        {
            get
            {
                dynamic result;
                if (_data.TryGetValue(index, out result))
                {
                    result = _data[index];
                }
                else
                {
                    result = null;
                }

                return result;
            }
            set
            {
                _data[index] = (dynamic)value;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _data[binder.Name];
            // We return true here because ViewDataDictionary returns null if the key is not
            // in the dictionary, so we simply pass on the returned value.
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // This cast should always succeed assuming TValue is dynamic.
            dynamic v = value;
            _data[binder.Name] = v;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes == null || indexes.Length != 1)
            {
                throw new ArgumentException("Invalid number of indexes");
            }

            object index = indexes[0];
            result = this[(string)index];
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes == null || indexes.Length != 1)
            {
                throw new ArgumentException("Invalid number of indexes");
            }

            object index = indexes[0];
            // This cast should always succeed assuming TValue is dynamic.
            this[(string)index] = value;
            return true;
        }
    }
}
