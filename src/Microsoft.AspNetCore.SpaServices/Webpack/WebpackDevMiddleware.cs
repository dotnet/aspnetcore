using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

// Putting in this namespace so it's always available whenever MapRoute is

namespace Microsoft.AspNetCore.Builder
{
    public static class WebpackDevMiddleware
    {
        private const string WebpackDevMiddlewareScheme = "http";
        private const string WebpackHotMiddlewareEndpoint = "/__webpack_hmr";
        private const string DefaultConfigFile = "webpack.config.js";

        public static void UseWebpackDevMiddleware(
            this IApplicationBuilder appBuilder,
            WebpackDevMiddlewareOptions options = null)
        {
            // Prepare options
            if (options == null)
            {
                options = new WebpackDevMiddlewareOptions();
            }

            // Validate options
            if (options.ReactHotModuleReplacement && !options.HotModuleReplacement)
            {
                throw new ArgumentException(
                    "To enable ReactHotModuleReplacement, you must also enable HotModuleReplacement.");
            }

            // Unlike other consumers of NodeServices, WebpackDevMiddleware dosen't share Node instances, nor does it
            // use your DI configuration. It's important for WebpackDevMiddleware to have its own private Node instance
            // because it must *not* restart when files change (if it did, you'd lose all the benefits of Webpack
            // middleware). And since this is a dev-time-only feature, it doesn't matter if the default transport isn't
            // as fast as some theoretical future alternative.
            var nodeServicesOptions = new NodeServicesOptions(appBuilder.ApplicationServices);
            nodeServicesOptions.WatchFileExtensions = new string[] {}; // Don't watch anything
            if (!string.IsNullOrEmpty(options.ProjectPath))
            {
                nodeServicesOptions.ProjectPath = options.ProjectPath;
            }

            var nodeServices = NodeServicesFactory.CreateNodeServices(nodeServicesOptions);

            // Get a filename matching the middleware Node script
            var script = EmbeddedResourceReader.Read(typeof(WebpackDevMiddleware),
                "/Content/Node/webpack-dev-middleware.js");
            var nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit

            // Tell Node to start the server hosting webpack-dev-middleware
            var devServerOptions = new
            {
                webpackConfigPath = Path.Combine(nodeServicesOptions.ProjectPath, options.ConfigFile ?? DefaultConfigFile),
                suppliedOptions = options
            };
            var devServerInfo =
                nodeServices.InvokeExportAsync<WebpackDevServerInfo>(nodeScript.FileName, "createWebpackDevServer",
                    JsonConvert.SerializeObject(devServerOptions)).Result;

            // Proxy the corresponding requests through ASP.NET and into the Node listener
            // Note that this is hardcoded to make requests to "localhost" regardless of the hostname of the
            // server as far as the client is concerned. This is because ConditionalProxyMiddlewareOptions is
            // the one making the internal HTTP requests, and it's going to be to some port on this machine
            // because aspnet-webpack hosts the dev server there. We can't use the hostname that the client
            // sees, because that could be anything (e.g., some upstream load balancer) and we might not be
            // able to make outbound requests to it from here.
            var proxyOptions = new ConditionalProxyMiddlewareOptions(WebpackDevMiddlewareScheme,
                "localhost", devServerInfo.Port.ToString());
            appBuilder.UseMiddleware<ConditionalProxyMiddleware>(devServerInfo.PublicPath, proxyOptions);

            // While it would be nice to proxy the /__webpack_hmr requests too, these return an EventStream,
            // and the Microsoft.AspNetCore.Proxy code doesn't handle that entirely - it throws an exception after
            // a while. So, just serve a 302 for those. But note that we must use the hostname that the client
            // sees, not "localhost", so that it works even when you're not running on localhost (e.g., Docker).
            appBuilder.Map(WebpackHotMiddlewareEndpoint, builder =>
            {
                builder.Use(next => ctx =>
                {
                    var hostname = ctx.Request.Host.Host;
                    ctx.Response.Redirect(
                        $"{WebpackDevMiddlewareScheme}://{hostname}:{devServerInfo.Port.ToString()}{WebpackHotMiddlewareEndpoint}");
                    return Task.FromResult(0);
                });
            });
        }

#pragma warning disable CS0649
        class WebpackDevServerInfo
        {
            public int Port { get; set; }
            public string PublicPath { get; set; }
        }
    }
#pragma warning restore CS0649
}