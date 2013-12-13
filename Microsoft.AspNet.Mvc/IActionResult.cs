using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionResult
    {
        Task ExecuteResultAsync(RequestContext context);
    }
}
