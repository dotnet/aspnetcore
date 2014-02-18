
namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // This is a temporary placeholder
    public class RouteValueValueProviderFactory : IValueProviderFactory
    {
        public IValueProvider CreateValueProvider(RequestContext context)
        {
            return new ValueProvider(context.RouteValues);
        }
    }
}
