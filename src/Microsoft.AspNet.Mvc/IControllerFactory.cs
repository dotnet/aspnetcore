using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public interface IControllerFactory
    {
        object CreateController(HttpContext context, string controllerName);

        void ReleaseController(object controller);
    }
}
