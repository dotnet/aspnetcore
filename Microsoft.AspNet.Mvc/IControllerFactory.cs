using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public interface IControllerFactory
    {
        object CreateController(IOwinContext context, string controllerName);

        void ReleaseController(object controller);
    }
}
