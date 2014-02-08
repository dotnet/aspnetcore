using System;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IControllerFactory _controllerFactory;

        public ActionInvokerProvider(IActionResultFactory actionResultFactory,
                                     IControllerFactory controllerFactory,
                                     IServiceProvider serviceProvider)
        {
            _actionResultFactory = actionResultFactory;
            _controllerFactory = controllerFactory;
            _serviceProvider = serviceProvider;
        }

        public IActionInvoker GetInvoker(RequestContext requestContext, RouteContext routeContext)
        {
            var controllerActionDescriptor = routeContext as ControllerActionRouteContext;

            if (controllerActionDescriptor != null)
            {
                return new ControllerActionInvoker(
                    requestContext,
                    controllerActionDescriptor,
                    _actionResultFactory,
                    _controllerFactory,
                    _serviceProvider);
            }

            return null;
        }
    }
}
