namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionRouteContext : RouteContext
    {
        public string ControllerName { get; set; }

        public string ActionName { get; set; }
    }
}
