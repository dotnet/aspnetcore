
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerFactory : IActionInvokerFactory
    {
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IActionInvokerProvider _actionInvokerProvider;
        private readonly IRouteContextProvider _routeContextProvider;

        public ActionInvokerFactory(IActionResultFactory actionResultFactory,
                                    IRouteContextProvider actionDescriptorProvider, 
                                    IActionInvokerProvider actionInvokerProvider)
        {
            _actionResultFactory = actionResultFactory;
            _routeContextProvider = actionDescriptorProvider;
            _actionInvokerProvider = actionInvokerProvider;
        }

        public IActionInvoker CreateInvoker(RequestContext requestContext)
        {
            RouteContext routeContext = _routeContextProvider.CreateDescriptor(requestContext);
            if (routeContext == null)
            {
                return null;
            }
           
            return _actionInvokerProvider.GetInvoker(requestContext, routeContext);
        }
    }
}
