using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionInvoker
    {
        Task InvokeActionAsync(string actionName);
    }
}
