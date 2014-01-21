
namespace Microsoft.AspNet.Mvc
{
    public class ActionDescriptorProvider : IActionDescriptorProvider
    {
        public ActionDescriptor CreateDescriptor(RequestContext requestContext)
        {
            string controllerName = requestContext.RouteData.GetRouteValue("controller");
            string actionName = requestContext.RouteData.GetRouteValue("action");

            return new ControllerBasedActionDescriptor
            {
                ControllerName = controllerName,
                ActionName = actionName
            };
        }
    }
}
