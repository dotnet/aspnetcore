
namespace Microsoft.AspNet.Mvc
{
    public interface IActionInvokerProvider
    {
        IActionInvoker GetInvoker(RequestContext requestContext, ActionDescriptor routeContext);
    }
}
