namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerFactory : IActionInvokerFactory
    {
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IActionInvokerProvider _actionInvokerProvider;
        private readonly IActionDescriptorProvider _routeContextProvider;

        public ActionInvokerFactory(IActionResultFactory actionResultFactory,
                                    IActionDescriptorProvider actionDescriptorProvider, 
                                    IActionInvokerProvider actionInvokerProvider)
        {
            _actionResultFactory = actionResultFactory;
            _routeContextProvider = actionDescriptorProvider;
            _actionInvokerProvider = actionInvokerProvider;
        }

        public IActionInvoker CreateInvoker(ActionContext actionContext)
        {
            return _actionInvokerProvider.GetInvoker(actionContext);
        }
    }
}
