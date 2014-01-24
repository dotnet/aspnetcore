
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerFactory : IActionInvokerFactory
    {
        private readonly IActionResultFactory _actionResultFactory;
        private readonly IActionDescriptorProvider _actionDescriptorProvider;
        private readonly IActionInvokerProvider _actionInvokerProvider;

        public ActionInvokerFactory(IActionResultFactory actionResultFactory,
                                    IActionDescriptorProvider actionDescriptorProvider, 
                                    IActionInvokerProvider actionInvokerProvider)
        {
            _actionResultFactory = actionResultFactory;
            _actionDescriptorProvider = actionDescriptorProvider;
            _actionInvokerProvider = actionInvokerProvider;
        }

        public IActionInvoker CreateInvoker(RequestContext requestContext)
        {
            ActionDescriptor descriptor = _actionDescriptorProvider.CreateDescriptor(requestContext);

            return _actionInvokerProvider.GetInvoker(requestContext, descriptor);
        }
    }
}
