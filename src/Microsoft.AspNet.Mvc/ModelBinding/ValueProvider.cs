
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // This is a temporary placeholder
    public class ValueProvider : IValueProvider
    {
        private readonly IDictionary<string, object> _values;
 
        public ValueProvider(IDictionary<string, object> values)
        {
            _values = values;
        }

        public bool ContainsPrefix(string key)
        {
            return _values.ContainsKey(key);
        }
    }
}
