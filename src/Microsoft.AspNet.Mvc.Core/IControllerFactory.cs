namespace Microsoft.AspNet.Mvc
{
    public interface IControllerFactory
    {
        object CreateController(ActionContext actionContext);
        void ReleaseController(object controller);
    }
}
