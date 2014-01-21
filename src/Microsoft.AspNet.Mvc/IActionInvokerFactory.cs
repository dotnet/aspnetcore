
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionInvokerFactory
    {
        IActionInvoker CreateInvoker(RequestContext requestContext);
    }
}
