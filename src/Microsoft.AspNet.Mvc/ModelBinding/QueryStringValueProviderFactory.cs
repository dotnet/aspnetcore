
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // This is a temporary placeholder
    public class QueryStringValueProviderFactory : IValueProviderFactory
    {
        public IValueProvider CreateValueProvider(RequestContext context)
        {
            return new QueryStringValueProvider(context.HttpContext.Request.Query);
        }

        private class QueryStringValueProvider : IValueProvider
        {
            private readonly IReadableStringCollection _values;

            public QueryStringValueProvider(IReadableStringCollection values)
            {
                _values = values;
            }

            public bool ContainsPrefix(string key)
            {
                return _values.Get(key) != null;
            }
        }
    }
}
