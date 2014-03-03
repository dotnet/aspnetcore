namespace Microsoft.AspNet.Mvc
{
    public class ActionResultFilterContext
    {
        public ActionResultFilterContext(ActionContext actionContext)
        {
            ActionContext = actionContext;
        }

        public ActionContext ActionContext { get; private set; }

        public IActionResult Result { get; set; }
    }
}
