
using System.Collections.Generic;
using System.Globalization;

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

        public ValueProviderResult GetValue([NotNull] string key)
        {
            object value;
            if (_values.TryGetValue(key, out value))
            {
                return new ValueProviderResult(value, value.ToString(), CultureInfo.InvariantCulture);
            }

            return null;
        }
    }
}
