
namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerProviderContext
    {
        public ActionInvokerProviderContext([NotNull]ActionContext actionContext)
        {
            ActionContext = actionContext;
        }

        public ActionContext ActionContext { get; private set; }

        public IActionInvoker Result { get; set; }
    }
}
