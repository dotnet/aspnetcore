
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryBasedValueProvider : IValueProvider
    {
        private readonly IDictionary<string, object> _values;

        public DictionaryBasedValueProvider(IDictionary<string, object> values)
        {
            _values = values;
        }

        public bool ContainsPrefix(string key)
        {
            return _values.ContainsKey(key);
        }

        public ValueProviderResult GetValue(string key)
        {
            if (key == null)
            {
                throw Error.ArgumentNull("key");
            }

            object value;
            if (_values.TryGetValue(key, out value))
            {
                return new ValueProviderResult(value, value.ToString(), CultureInfo.InvariantCulture);
            }

            return null;
        }
    }
}
