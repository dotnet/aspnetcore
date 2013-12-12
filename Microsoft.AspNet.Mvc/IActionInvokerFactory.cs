
namespace Microsoft.AspNet.Mvc
{
    public interface IActionInvokerFactory
    {
        IActionInvoker CreateInvoker(ControllerContext context);
    }
}
