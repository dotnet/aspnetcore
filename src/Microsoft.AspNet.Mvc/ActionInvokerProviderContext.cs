using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionInvokerProviderContext
    {
        public ActionInvokerProviderContext(ActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException("actionContext");
            }

            ActionContext = actionContext;
        }

        public ActionContext ActionContext { get; private set; }

        public IActionInvoker Result { get; set; }
    }
}
