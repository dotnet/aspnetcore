using System;

namespace Microsoft.AspNet.Mvc
{
    public class ExceptionFilterContext
    {
        public ExceptionFilterContext(ActionContext actionContext, Exception exception)
        {
            ActionContext = actionContext;
            Exception = exception;
        }

        // TODO: Should we let the exception mutate in the pipeline. MVC lets you do that.
        public virtual Exception Exception { get; set; }

        public virtual ActionContext ActionContext { get; private set; }

        public virtual IActionResult Result { get; set; }
    }
}
