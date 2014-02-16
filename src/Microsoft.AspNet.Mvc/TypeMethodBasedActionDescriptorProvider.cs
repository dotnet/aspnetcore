namespace Microsoft.AspNet.Mvc
{
    public class TypeMethodBasedActionDescriptorProvider : IActionDescriptorProvider
    {
        public ActionDescriptor CreateDescriptor(RequestContext requestContext)
        {
            var controllerName = (string)requestContext.RouteValues["controller"];
            var actionName = (string)requestContext.RouteValues["action"];

            return new TypeMethodBasedActionDescriptor()
            {
                ControllerName = controllerName,
                ActionName = actionName
            };
        }
    }
}
