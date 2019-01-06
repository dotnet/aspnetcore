using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.SpaServices.Webpack
{
    internal class ConditionalProxyMiddlewareOptions
    {
        public ConditionalProxyMiddlewareOptions(string scheme, string host, string port, TimeSpan requestTimeout, Action<HttpContext> onPrepareResponse = null)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
            RequestTimeout = requestTimeout;
            OnPrepareResponse = onPrepareResponse;
        }

        public string Scheme { get; }
        public string Host { get; }
        public string Port { get; }
        public TimeSpan RequestTimeout { get; }
        public Action<HttpContext> OnPrepareResponse { get; set; }
    }
}
