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

        public int Order
        {
            get { return 0; }
        }

        public void Invoke(ActionInvokerProviderContext context, Action callNext)
        {
            var ad = context.ActionContext.ActionDescriptor as TypeMethodBasedActionDescriptor;

            if (ad != null)
            {
                context.ActionInvoker = new TypeMethodBasedActionInvoker(
                    context.ActionContext,
                    ad,
                    _actionResultFactory,
                    _controllerFactory,
                    _serviceProvider);
            }

            callNext();
        }


    }
}
