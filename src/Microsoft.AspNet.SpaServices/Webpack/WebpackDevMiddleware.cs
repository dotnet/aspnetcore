using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.Proxy;
using Microsoft.AspNet.SpaServices.Webpack;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

// Putting in this namespace so it's always available whenever MapRoute is
namespace Microsoft.AspNet.Builder
{
    public static class WebpackDevMiddleware
    {
        const string WebpackDevMiddlewareHostname = "localhost";
        const string WebpackHotMiddlewareEndpoint = "/__webpack_hmr";

        public static void UseWebpackDevMiddleware(this IApplicationBuilder appBuilder, WebpackDevMiddlewareOptions options = null) {
            // Validate options
            if (options != null) {
                if (options.ReactHotModuleReplacement && !options.HotModuleReplacement) {
                    throw new ArgumentException("To enable ReactHotModuleReplacement, you must also enable HotModuleReplacement.");
                }
            }

            // Unlike other consumers of NodeServices, WebpackDevMiddleware dosen't share Node instances, nor does it
            // use your DI configuration. It's important for WebpackDevMiddleware to have its own private Node instance
            // because it must *not* restart when files change (if it did, you'd lose all the benefits of Webpack
            // middleware). And since this is a dev-time-only feature, it doesn't matter if the default transport isn't
            // as fast as some theoretical future alternative.
            var appEnv = (IApplicationEnvironment)appBuilder.ApplicationServices.GetService(typeof(IApplicationEnvironment));
            var nodeServices = Configuration.CreateNodeServices(new NodeServicesOptions {
                HostingModel = NodeHostingModel.Http,
                ProjectPath = appEnv.ApplicationBasePath,
                WatchFileExtensions = new string[] {} // Don't watch anything
            });

            // Get a filename matching the middleware Node script
            var script = EmbeddedResourceReader.Read(typeof (WebpackDevMiddleware), "/Content/Node/webpack-dev-middleware.js");
            var nodeScript = new StringAsTempFile(script); // Will be cleaned up on process exit

            // Tell Node to start the server hosting webpack-dev-middleware
            var devServerOptions = new {
                webpackConfigPath = Path.Combine(appEnv.ApplicationBasePath, "webpack.config.js"),
                suppliedOptions = options ?? new WebpackDevMiddlewareOptions()
            };
            var devServerInfo = nodeServices.InvokeExport<WebpackDevServerInfo>(nodeScript.FileName, "createWebpackDevServer", JsonConvert.SerializeObject(devServerOptions)).Result;

            // Proxy the corresponding requests through ASP.NET and into the Node listener
            appBuilder.Map(devServerInfo.PublicPath, builder => {
                builder.RunProxy(new ProxyOptions {
                    Host = WebpackDevMiddlewareHostname,
                    Port = devServerInfo.Port.ToString()
                });
            });

            // While it would be nice to proxy the /__webpack_hmr requests too, these return an EventStream,
            // and the Microsoft.Aspnet.Proxy code doesn't handle that entirely - it throws an exception after
            // a while. So, just serve a 302 for those.
            appBuilder.Map(WebpackHotMiddlewareEndpoint, builder => {
                builder.Use(next => async ctx => {
                    ctx.Response.Redirect($"http://localhost:{ devServerInfo.Port.ToString() }{ WebpackHotMiddlewareEndpoint }");
                    await Task.Yield();
                });
            });
        }

        #pragma warning disable CS0649
        class WebpackDevServerInfo {
            public int Port;
            public string PublicPath;
        }
        #pragma warning restore CS0649
    }
}
