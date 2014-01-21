using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class HttpResponseMessageActionResult : IActionResult
    {
        public HttpResponseMessage ResponseMessage { get; set; }

        public HttpResponseMessageActionResult(HttpResponseMessage responseMessage)
        {
            ResponseMessage = responseMessage;
        }

        public async Task ExecuteResultAsync(RequestContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = (int)ResponseMessage.StatusCode;

            foreach (var responseHeader in ResponseMessage.Headers)
            {
                response.Headers.AppendValues(responseHeader.Key, responseHeader.Value.ToArray());
            }

            var content = ResponseMessage.Content;
            if (content != null)
            {
                foreach (var responseHeader in content.Headers)
                {
                    response.Headers.AppendValues(responseHeader.Key, responseHeader.Value.ToArray());
                }

                await content.CopyToAsync(response.Body);
            }
        }
    }
}
