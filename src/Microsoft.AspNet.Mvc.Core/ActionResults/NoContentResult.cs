using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class NoContentResult : ActionResult
    {
        public override void ExecuteResult([NotNull] ActionContext context)
        {
            HttpResponse response = context.HttpContext.Response;

#if NET45
            response.StatusCode = (int)HttpStatusCode.NoContent;
#else
            response.StatusCode = 204;
#endif
        }
    }
}
