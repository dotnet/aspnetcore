using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.Proxy;
using Microsoft.AspNet.SpaServices;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

// Putting in this namespace so it's always available whenever MapRoute is
namespace Microsoft.AspNet.Builder
{
    public static class WebpackDevMiddleware
    {
        const string WebpackDevMiddlewareHostname = "localhost";
        const string WebpackHotMiddlewareEndpoint = "/__webpack_hmr";

        static INodeServices fallbackNodeServices; // Used only if no INodeServices was registered with DI

        public static void UseWebpackDevMiddleware(this IApplicationBuilder appBuilder, WebpackDevMiddlewareOptions options = null) {
            // Validate options
            if (options != null) {
                if (options.ReactHotModuleReplacement && !options.HotModuleReplacement) {
                    throw new ArgumentException("To enable ReactHotModuleReplacement, you must also enable HotModuleReplacement.");
                }
            }

            // Get the NodeServices instance from DI
            var nodeServices = (INodeServices)appBuilder.ApplicationServices.GetService(typeof (INodeServices)) ?? fallbackNodeServices;
            
            // Consider removing the following. Having it means you can get away with not putting app.AddNodeServices()
            // in your startup file, but then again it might be confusing that you don't need to.
            var appEnv = (IApplicationEnvironment)appBuilder.ApplicationServices.GetService(typeof(IApplicationEnvironment));
            if (nodeServices == null) {
                nodeServices = fallbackNodeServices = Configuration.CreateNodeServices(NodeHostingModel.Http, appEnv.ApplicationBasePath);
            }
            
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
        
        class WebpackDevServerInfo {
            public int Port;
            public string PublicPath;
        }
    }
}