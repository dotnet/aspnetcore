using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerFactory : IActionInvokerFactory
    {
        private readonly INestedProviderManager<ActionInvokerProviderContext> _actionInvokerProvider;

        public ActionInvokerFactory(INestedProviderManager<ActionInvokerProviderContext> actionInvokerProvider)
        {
            _actionInvokerProvider = actionInvokerProvider;
        }

        public IActionInvoker CreateInvoker(ActionContext actionContext)
        {
            var context = new ActionInvokerProviderContext(actionContext);
            _actionInvokerProvider.Invoke(context);
            return context.ActionInvoker;
        }
    }
}
