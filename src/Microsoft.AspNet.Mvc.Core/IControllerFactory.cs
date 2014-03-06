
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public interface IControllerFactory
    {
        object CreateController(ActionContext actionContext, ModelStateDictionary modelState);

        void ReleaseController(object controller);
    }
}
