using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.AngularServices
{
    public static class PrimeCacheHelper
    {
        public static async Task<HtmlString> PrimeCache(this IHtmlHelper html, string url)
        {
            // TODO: Consider deduplicating the PrimeCache calls (that is, if there are multiple requests to precache
            // the same URL, only return nonempty for one of them). This will make it easier to auto-prime-cache any
            // HTTP requests made during server-side rendering, without risking unnecessary duplicate requests.

            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(url));
            }

            try
            {
                var request = html.ViewContext.HttpContext.Request;
                var baseUriString = string.Concat(
                    request.Scheme,
                    "://",
                    request.Host.ToUriComponent(),
                    request.PathBase.ToUriComponent(),
                    request.Path.ToUriComponent(),
                    request.QueryString.ToUriComponent());
                var fullUri = new Uri(new Uri(baseUriString), url);
                var response = await new HttpClient().GetAsync(fullUri.ToString());
                var responseBody = await response.Content.ReadAsStringAsync();
                return new HtmlString(FormatAsScript(url, response.StatusCode, responseBody));
            }
            catch (Exception ex)
            {
                var logger = (ILogger)html.ViewContext.HttpContext.RequestServices.GetService(typeof(ILogger));
                logger?.LogWarning("Error priming cache for URL: " + url, ex);
                return new HtmlString(string.Empty);
            }
        }

        private static string FormatAsScript(string url, HttpStatusCode responseStatusCode, string responseBody)
        {
            var preCachedUrl = JsonConvert.SerializeObject(url);
            var preCachedJson = JsonConvert.SerializeObject(new { statusCode = responseStatusCode, body = responseBody });
            return "<script>"
                + "window.__preCachedResponses = window.__preCachedResponses || {};"
                + $"window.__preCachedResponses[{preCachedUrl}] = {preCachedJson};"
                + "</script>";
        }
    }
}