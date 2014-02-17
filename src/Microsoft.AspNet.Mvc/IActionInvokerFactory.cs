namespace Microsoft.AspNet.Mvc
{
    public interface IActionInvokerFactory
    {
        IActionInvoker CreateInvoker(ActionContext actionContext);
    }
}
