using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerProvider : IActionInvokerProvider
    {
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IControllerFactory _controllerFactory;
        private readonly IActionBindingContextProvider _bindingProvider;

        public ActionInvokerProvider(IActionResultFactory actionResultFactory,
                                     IControllerFactory controllerFactory,
                                     IActionBindingContextProvider bindingProvider,
                                     IServiceProvider serviceProvider)
        {
            _actionResultFactory = actionResultFactory;
            _controllerFactory = controllerFactory;
            _bindingProvider = bindingProvider;
            _serviceProvider = serviceProvider;
        }

        public int Order
        {
            get { return 0; }
        }

        public void Invoke(ActionInvokerProviderContext context, Action callNext)
        {
            var actionDescriptor = context.ActionContext.ActionDescriptor as ReflectedActionDescriptor;

            if (actionDescriptor != null)
            {
                context.Result = new ReflectedActionInvoker(
                                    context.ActionContext,
                                    actionDescriptor,
                                    _actionResultFactory,
                                    _controllerFactory,
                                    _bindingProvider,
                                    _serviceProvider);
            }

            callNext();
        }
    }
}
