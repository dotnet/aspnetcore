namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionBasedRouteContextProvider : IRouteContextProvider
    {
        public RouteContext CreateDescriptor(RequestContext requestContext)
        {
            string controllerName = requestContext.RouteData.GetRouteValue("controller");
            string actionName = requestContext.RouteData.GetRouteValue("action");

            return new ControllerActionRouteContext
            {
                ControllerName = controllerName,
                ActionName = actionName
            };
        }
    }
}
