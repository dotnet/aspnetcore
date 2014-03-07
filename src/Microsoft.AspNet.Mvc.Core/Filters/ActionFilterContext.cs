using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class ActionFilterContext
    {
        public ActionFilterContext(ActionContext actionContext,
                                   IDictionary<string, object> actionArguments)
        {
            ActionContext = actionContext;
            ActionArguments = actionArguments;
        }

        public virtual IDictionary<string, object> ActionArguments { get; private set; }

        public virtual ActionContext ActionContext { get; private set; }

        public virtual IActionResult Result { get; set; }
    }
}
