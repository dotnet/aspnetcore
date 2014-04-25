using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class HttpStatusCodeResult : ActionResult
    {
        private int _statusCode;

        public HttpStatusCodeResult(int statusCode)
        {
            _statusCode = statusCode;
        }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            context.HttpContext.Response.StatusCode = _statusCode;
        }
    }
}
