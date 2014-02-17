namespace Microsoft.AspNet.Mvc
{
    public interface IActionInvokerProvider
    {
        IActionInvoker GetInvoker(ActionContext actionContext);
    }
}
