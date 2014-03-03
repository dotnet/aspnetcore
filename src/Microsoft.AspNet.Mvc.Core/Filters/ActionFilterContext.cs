using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class ActionFilterContext
    {
        public ActionFilterContext(ActionContext actionContext, IDictionary<string, object> actionParameters)
        {
            ActionContext = actionContext;
            ActionParameters = actionParameters;
        }

        public virtual IDictionary<string, object> ActionParameters { get; private set; }

        public virtual ActionContext ActionContext { get; private set; }

        public virtual IActionResult Result { get; set; }
    }
}
