
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RouteValueValueProviderFactory : IValueProviderFactory
    {
        public Task<IValueProvider> GetValueProviderAsync(RequestContext requestContext)
        {
            var valueProvider = new DictionaryBasedValueProvider(requestContext.RouteValues);
            return Task.FromResult<IValueProvider>(valueProvider);
        }
    }
}
