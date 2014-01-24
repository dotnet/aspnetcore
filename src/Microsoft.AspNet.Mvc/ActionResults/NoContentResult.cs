using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class NoContentResult : IActionResult
    {
        public async Task ExecuteResultAsync(RequestContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpResponse response = context.HttpContext.Response;

            response.StatusCode = (int)HttpStatusCode.NoContent;

            await Task.FromResult(false);

            return;
        }
    }
}
