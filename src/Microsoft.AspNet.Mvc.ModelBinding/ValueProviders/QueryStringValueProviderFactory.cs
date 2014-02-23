using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class QueryStringValueProviderFactory : IValueProviderFactory
    {
        private static readonly object _cacheKey = new object();

        public IValueProvider GetValueProvider(RequestContext requestContext)
        {
            if (requestContext == null)
            {
                throw Error.ArgumentNull("requestContext");
            }

            // Process the query string once-per request. 
            IDictionary<object, object> storage = requestContext.HttpContext.Items;
            object value;
            if (!storage.TryGetValue(_cacheKey, out value))
            {
                var provider = new QueryStringValueProvider(requestContext.HttpContext, CultureInfo.InvariantCulture);
                storage[_cacheKey] = provider;
                return provider;
            }

            return (QueryStringValueProvider)value;
        }
    }
}
