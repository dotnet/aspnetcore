namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionBasedRouteContextProvider : IRouteContextProvider
    {
        public RouteContext CreateDescriptor(RequestContext requestContext)
        {
            var controllerName = (string)requestContext.RouteValues["controller"];
            var actionName = (string)requestContext.RouteValues["action"];

            return new ControllerActionRouteContext
            {
                ControllerName = controllerName,
                ActionName = actionName
            };
        }
    }
}
