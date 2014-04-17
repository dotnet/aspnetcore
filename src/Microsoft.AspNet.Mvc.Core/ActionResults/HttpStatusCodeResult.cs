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

        #pragma warning disable 1998
        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = _statusCode;
        }
        #pragma warning restore 1998
    }
}
