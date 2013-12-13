using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class HttpStatusCodeResult : IActionResult
    {
        private int _statusCode;

        public HttpStatusCodeResult(int statusCode)
        {
            _statusCode = statusCode;
        }

        public async Task ExecuteResultAsync(RequestContext context)
        {
            context.HttpContext.Response.StatusCode = _statusCode;
        }
    }
}
