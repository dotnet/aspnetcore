
namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RouteValueValueProviderFactory : IValueProviderFactory
    {
        public IValueProvider GetValueProvider(RequestContext requestContext)
        {
            return new DictionaryBasedValueProvider(requestContext.RouteValues);
        }
    }
}
