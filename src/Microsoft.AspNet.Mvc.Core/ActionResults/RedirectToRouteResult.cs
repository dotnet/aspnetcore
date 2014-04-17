using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    public class RedirectToRouteResult : IActionResult
    {
        public RedirectToRouteResult([NotNull] IUrlHelper urlHelper, IDictionary<string, object> routeValues)
            : this(urlHelper, routeValues, permanent: false)
        {
        }

        public RedirectToRouteResult([NotNull] IUrlHelper urlHelper, 
                                        IDictionary<string, object> routeValues, bool permanent)
        {
            UrlHelper = urlHelper;
            RouteValues = routeValues;
            Permanent = permanent;
        }

        public IUrlHelper UrlHelper { get; private set; }

        public IDictionary<string, object> RouteValues { get; private set; }

        public bool Permanent { get; private set; }

        #pragma warning disable 1998
        public async Task ExecuteResultAsync([NotNull] ActionContext context)
        {
            var destinationUrl = UrlHelper.RouteUrl(RouteValues);

            if (string.IsNullOrEmpty(destinationUrl))
            {
                throw new InvalidOperationException(Resources.NoRoutesMatched);
            }

            context.HttpContext.Response.Redirect(destinationUrl, Permanent);
        }
        #pragma warning restore 1998
    }
}
