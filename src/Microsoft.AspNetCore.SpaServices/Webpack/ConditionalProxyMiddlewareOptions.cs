namespace Microsoft.AspNetCore.SpaServices.Webpack
{
    internal class ConditionalProxyMiddlewareOptions
    {
        public ConditionalProxyMiddlewareOptions(string scheme, string host, string port)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
        }

        public string Scheme { get; }
        public string Host { get; }
        public string Port { get; }
    }
}