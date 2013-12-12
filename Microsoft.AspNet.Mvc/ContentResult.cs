using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class ContentResult : IActionResult
    {
        public string Content { get; set; }

        public Encoding ContentEncoding { get; set; }

        public string ContentType { get; set; }

        public async Task ExecuteResultAsync(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            IOwinResponse response = context.HttpContext.Response;

            if (!String.IsNullOrEmpty(ContentType))
            {
                response.ContentType = ContentType;
            }

            //if (ContentEncoding != null)
            //{
            //    response.ContentEncoding = ContentEncoding;
            //}

            if (Content != null)
            {
                await response.WriteAsync(Content);
            }
        }
    }
}
