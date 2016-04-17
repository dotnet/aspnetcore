using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.AspNet.AngularServices {
    public static class PrimeCacheHelper {
        public static async Task<HtmlString> PrimeCache(this IHtmlHelper html, string url) {
            // TODO: Consider deduplicating the PrimeCache calls (that is, if there are multiple requests to precache
            // the same URL, only return nonempty for one of them). This will make it easier to auto-prime-cache any
            // HTTP requests made during server-side rendering, without risking unnecessary duplicate requests.

            if (string.IsNullOrEmpty(url)) {
                throw new ArgumentException("Value cannot be null or empty", nameof(url));
            }

            try {
                var request = html.ViewContext.HttpContext.Request;
                var baseUri = new Uri(string.Concat(request.Scheme, "://", request.Host.ToUriComponent(), request.PathBase.ToUriComponent(), request.Path.ToUriComponent(), request.QueryString.ToUriComponent()));
                var fullUri = new Uri(baseUri, url);
                var response = await new HttpClient().GetAsync(fullUri.ToString());
                var responseBody = await response.Content.ReadAsStringAsync();
                return new HtmlString(FormatAsScript(url, response.StatusCode, responseBody));
            } catch (Exception ex) {
                var logger = (ILogger)html.ViewContext.HttpContext.ApplicationServices.GetService(typeof (ILogger));
                if (logger != null) {
                    logger.LogWarning("Error priming cache for URL: " + url, ex);
                }
                return new HtmlString(string.Empty);
            }
        }

        private static string FormatAsScript(string url, HttpStatusCode responseStatusCode, string responseBody)
        {
            return string.Format(@"<script>window.__preCachedResponses = window.__preCachedResponses || {{}}; window.__preCachedResponses[{0}] = {1};</script>",
                JsonConvert.SerializeObject(url),
                JsonConvert.SerializeObject(new { statusCode = responseStatusCode, body = responseBody })
            );
        }
    }
}
