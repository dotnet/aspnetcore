using System;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectResult : IActionResult
    {
        public RedirectResult(string url)
            : this(url, permanent: false)
        {
        }

        public RedirectResult(string url, bool permanent)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "url");
            }

            Permanent = permanent;
            Url = url;
        }

        public bool Permanent { get; private set; }

        public string Url { get; private set; }

        #pragma warning disable 1998
        public async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            // It is redirected directly to the input URL.
            // We would use the context to construct the full URL,
            // only when relative URLs are supported. (Issue - WEBFX-202)
            context.HttpContext.Response.Redirect(Url, Permanent);
        }
        #pragma warning restore 1998
    }
}