using System;

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

        public IActionInvoker GetInvoker(ActionContext actionContext)
        {
            var ad = actionContext.ActionDescriptor as TypeMethodBasedActionDescriptor;

            if (ad != null)
            {
                return new TypeMethodBasedActionInvoker(
                    actionContext,
                    ad,
                    _actionResultFactory,
                    _controllerFactory,
                    _serviceProvider);
            }

            return null;
        }
    }
}
