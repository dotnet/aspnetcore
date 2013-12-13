using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerProvider : IActionInvokerProvider
    {
        private IActionResultFactory _actionResultFactory;
        private IServiceProvider _serviceProvider;

        public ActionInvokerProvider(IActionResultFactory actionResultFactory,
                                     IServiceProvider serviceProvider)
        {
            _actionResultFactory = actionResultFactory;
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
                    _serviceProvider);
            }

            return null;
        }
    }
}
