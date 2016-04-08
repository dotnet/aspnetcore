using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.SpaServices.Webpack
{
    // Based on https://github.com/aspnet/Proxy/blob/dev/src/Microsoft.AspNetCore.Proxy/ProxyMiddleware.cs
    // Differs in that, if the proxied request returns a 404, we pass through to the next middleware in the chain
    // This is useful for Webpack middleware, because it lets you fall back on prebuilt files on disk for
    // chunks not exposed by the current Webpack config (e.g., DLL/vendor chunks).
    internal class ConditionalProxyMiddleware {
        private RequestDelegate next;
        private ConditionalProxyMiddlewareOptions options;
        private HttpClient httpClient;
        private string pathPrefix;

        public ConditionalProxyMiddleware(RequestDelegate next, string pathPrefix, ConditionalProxyMiddlewareOptions options)
        {
            this.next = next;
            this.pathPrefix = pathPrefix;
            this.options = options;
            this.httpClient = new HttpClient(new HttpClientHandler());
        }
        
        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(this.pathPrefix)) {
                var didProxyRequest = await PerformProxyRequest(context);
                if (didProxyRequest) {
                    return;
                }
            }
            
            // Not a request we can proxy
            await this.next.Invoke(context);
        }
        
        private async Task<bool> PerformProxyRequest(HttpContext context) {
            var requestMessage = new HttpRequestMessage();

            // Copy the request headers
            foreach (var header in context.Request.Headers) {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null) {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
            
            requestMessage.Headers.Host = options.Host + ":" + options.Port;
            var uriString = $"{options.Scheme}://{options.Host}:{options.Port}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
            requestMessage.RequestUri = new Uri(uriString);
            requestMessage.Method = new HttpMethod(context.Request.Method);

            using (var responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted)) {
                if (responseMessage.StatusCode == HttpStatusCode.NotFound) {
                    // Let some other middleware handle this
                    return false;
                }
                
                // We can handle this
                context.Response.StatusCode = (int)responseMessage.StatusCode;                
                foreach (var header in responseMessage.Headers) {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in responseMessage.Content.Headers) {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
                context.Response.Headers.Remove("transfer-encoding");
                await responseMessage.Content.CopyToAsync(context.Response.Body);
                return true;
            }
        }
    }
    
    internal class ConditionalProxyMiddlewareOptions {
        public string Scheme { get; private set; }
        public string Host { get; private set; }
        public string Port { get; private set; }
        
        public ConditionalProxyMiddlewareOptions(string scheme, string host, string port) {
            this.Scheme = scheme;
            this.Host = host;
            this.Port = port;
        }
    }
}
