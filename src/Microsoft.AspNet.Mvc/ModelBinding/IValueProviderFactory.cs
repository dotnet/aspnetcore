

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IValueProviderFactory
    {
        IValueProvider CreateValueProvider(RequestContext context);
    }
}
