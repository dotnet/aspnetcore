using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionDescriptorProvider
    {
        ActionDescriptor CreateDescriptor(RequestContext requestContext);
    }
}
