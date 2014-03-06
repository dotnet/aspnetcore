using System;

namespace Microsoft.AspNet.Mvc
{
    // TODO: For now we have not implemented the ExceptionFilter pipeline, leaving this in until we decide if we are going
    // down this path or implementing an ExceptionFilterAttribute being all three filter types with a higher scope.
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
