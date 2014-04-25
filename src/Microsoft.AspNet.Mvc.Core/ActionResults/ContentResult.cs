using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public class ContentResult : ActionResult
    {
        public string Content { get; set; }

        public Encoding ContentEncoding { get; set; }

        public string ContentType { get; set; }

        public override async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
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
