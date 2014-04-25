using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryBasedValueProvider : IValueProvider
    {
        private readonly IDictionary<string, object> _values;

        public DictionaryBasedValueProvider(IDictionary<string, object> values)
        {
            _values = values;
        }

        public Task<bool> ContainsPrefixAsync(string key)
        {
            return Task.FromResult(_values.ContainsKey(key));
        }

        public Task<ValueProviderResult> GetValueAsync([NotNull] string key)
        {
            object value;
            ValueProviderResult result;
            if (_values.TryGetValue(key, out value))
            {
                var attemptedValue = value != null ? value.ToString() : null;
                result = new ValueProviderResult(value, attemptedValue, CultureInfo.InvariantCulture);
            }
            else
            {
                result = null;
            }
            
            return Task.FromResult(result);
        }
    }
}
