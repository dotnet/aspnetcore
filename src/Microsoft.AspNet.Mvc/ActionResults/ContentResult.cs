using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class ContentResult : IActionResult
    {
        public string Content { get; set; }

        public Encoding ContentEncoding { get; set; }

        public string ContentType { get; set; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpResponse response = context.HttpContext.Response;

            if (!String.IsNullOrEmpty(ContentType))
            {
                response.ContentType = ContentType;
            }
           
            if (Content != null)
            {
                await response.WriteAsync(Content);
            }
        }
    }
}
