using System;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IControllerFactory _controrllerFactory;

        public ActionInvokerProvider(IActionResultFactory actionResultFactory,
                                     IControllerFactory controllerFactory,
                                     IServiceProvider serviceProvider)
        {
            _actionResultFactory = actionResultFactory;
            _controrllerFactory = controllerFactory;
            _serviceProvider = serviceProvider;
        }

        public IActionInvoker GetInvoker(RequestContext requestContext, ActionDescriptor descriptor)
        {
            var controllerActionDescriptor = descriptor as ControllerBasedActionDescriptor;

            if (controllerActionDescriptor != null)
            {
                return new ControllerActionInvoker(
                    requestContext,
                    controllerActionDescriptor,
                    _actionResultFactory,
                    _controrllerFactory,
                    _serviceProvider);
            }

            return null;
        }
    }
}
