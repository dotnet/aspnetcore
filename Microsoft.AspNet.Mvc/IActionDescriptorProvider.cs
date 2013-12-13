using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionDescriptorProvider
    {
        ActionDescriptor CreateDescriptor(RequestContext requestContext);
    }
}
