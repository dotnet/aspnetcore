using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class QueryStringValueProviderFactory : IValueProviderFactory
    {
        private static readonly object _cacheKey = new object();

        public Task<IValueProvider> GetValueProviderAsync(RequestContext requestContext)
        {
            if (requestContext == null)
            {
                throw Error.ArgumentNull("requestContext");
            }

            // Process the query collection once-per request. 
            var storage = requestContext.HttpContext.Items;
            object value;
            IValueProvider provider;
            if (!storage.TryGetValue(_cacheKey, out value))
            {
                var queryCollection = requestContext.HttpContext.Request.Query;
                provider = new ReadableStringCollectionValueProvider(queryCollection, CultureInfo.InvariantCulture);
                storage[_cacheKey] = provider;
            }
            else
            {
                provider = (ReadableStringCollectionValueProvider)value;
            }
            return Task.FromResult(provider);
        }
    }
}
