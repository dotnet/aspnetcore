namespace Microsoft.AspNet.Mvc
{
    public class ActionResultFilterContext
    {
        public ActionResultFilterContext(ActionContext actionContext, IActionResult initialResult)
        {
            ActionContext = actionContext;
            Result = initialResult;
        }

        public ActionContext ActionContext { get; private set; }

        public IActionResult Result { get; set; }
    }
}
