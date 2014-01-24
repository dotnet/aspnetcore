
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionInvokerFactory
    {
        IActionInvoker CreateInvoker(RequestContext requestContext);
    }
}
