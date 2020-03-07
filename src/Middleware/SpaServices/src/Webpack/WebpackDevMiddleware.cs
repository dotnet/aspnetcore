// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods that can be used to enable Webpack dev middleware support.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public static class WebpackDevMiddleware
    {
        private const string DefaultConfigFile = "webpack.config.js";

        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            // Note that the aspnet-webpack JS code specifically expects options to be serialized with
            // PascalCase property names, so it's important to be explicit about this contract resolver
            ContractResolver = new DefaultContractResolver(),

            TypeNameHandling = TypeNameHandling.None
        };

        /// <summary>
        /// Enables Webpack dev middleware support. This hosts an instance of the Webpack compiler in memory
        /// in your application so that you can always serve up-to-date Webpack-built resources without having
        /// to run the compiler manually. Since the Webpack compiler instance is retained in memory, incremental
        /// compilation is vastly faster that re-running the compiler from scratch.
        ///
        /// Incoming requests that match Webpack-built files will be handled by returning the Webpack compiler
        /// output directly, regardless of files on disk. If compilation is in progress when the request arrives,
        /// the response will pause until updated compiler output is ready.
        /// </summary>
        /// <param name="appBuilder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">Options for configuring the Webpack compiler instance.</param>
        [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
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
            nodeServicesOptions.WatchFileExtensions = new string[] { }; // Don't watch anything
            if (!string.IsNullOrEmpty(options.ProjectPath))
            {
                nodeServicesOptions.ProjectPath = options.ProjectPath;
            }

            if (options.EnvironmentVariables != null)
            {
                foreach (var kvp in options.EnvironmentVariables)
                {
                    nodeServicesOptions.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }

            var nodeServices = NodeServicesFactory.CreateNodeServices(nodeServicesOptions);

            // Get a filename matching the middleware Node script
            var script = EmbeddedResourceReader.Read(typeof(WebpackDevMiddleware),
                "/Content/Node/webpack-dev-middleware.js");
            var nodeScript = new StringAsTempFile(script, nodeServicesOptions.ApplicationStoppingToken); // Will be cleaned up on process exit

            // Ideally, this would be relative to the application's PathBase (so it could work in virtual directories)
            // but it's not clear that such information exists during application startup, as opposed to within the context
            // of a request.
            var hmrEndpoint = !string.IsNullOrEmpty(options.HotModuleReplacementEndpoint)
                ? options.HotModuleReplacementEndpoint
                : "/__webpack_hmr"; // Matches webpack's built-in default

            // Tell Node to start the server hosting webpack-dev-middleware
            var devServerOptions = new
            {
                webpackConfigPath = Path.Combine(nodeServicesOptions.ProjectPath, options.ConfigFile ?? DefaultConfigFile),
                suppliedOptions = options,
                understandsMultiplePublicPaths = true,
                hotModuleReplacementEndpointUrl = hmrEndpoint
            };
            var devServerInfo =
                nodeServices.InvokeExportAsync<WebpackDevServerInfo>(nodeScript.FileName, "createWebpackDevServer",
                    JsonConvert.SerializeObject(devServerOptions, jsonSerializerSettings)).Result;

            // If we're talking to an older version of aspnet-webpack, it will return only a single PublicPath,
            // not an array of PublicPaths. Handle that scenario.
            if (devServerInfo.PublicPaths == null)
            {
                devServerInfo.PublicPaths = new[] { devServerInfo.PublicPath };
            }

            // Proxy the corresponding requests through ASP.NET and into the Node listener
            // Anything under /<publicpath> (e.g., /dist) is proxied as a normal HTTP request with a typical timeout (100s is the default from HttpClient),
            // plus /__webpack_hmr is proxied with infinite timeout, because it's an EventSource (long-lived request).
            foreach (var publicPath in devServerInfo.PublicPaths)
            {
                appBuilder.UseProxyToLocalWebpackDevMiddleware(publicPath + hmrEndpoint, devServerInfo.Port, Timeout.InfiniteTimeSpan);
                appBuilder.UseProxyToLocalWebpackDevMiddleware(publicPath, devServerInfo.Port, TimeSpan.FromSeconds(100));
            }
        }

        private static void UseProxyToLocalWebpackDevMiddleware(this IApplicationBuilder appBuilder, string publicPath, int proxyToPort, TimeSpan requestTimeout)
        {
            // Note that this is hardcoded to make requests to "localhost" regardless of the hostname of the
            // server as far as the client is concerned. This is because ConditionalProxyMiddlewareOptions is
            // the one making the internal HTTP requests, and it's going to be to some port on this machine
            // because aspnet-webpack hosts the dev server there. We can't use the hostname that the client
            // sees, because that could be anything (e.g., some upstream load balancer) and we might not be
            // able to make outbound requests to it from here.
            // Also note that the webpack HMR service always uses HTTP, even if your app server uses HTTPS,
            // because the HMR service has no need for HTTPS (the client doesn't see it directly - all traffic
            // to it is proxied), and the HMR service couldn't use HTTPS anyway (in general it wouldn't have
            // the necessary certificate).
            var proxyOptions = new ConditionalProxyMiddlewareOptions(
                "http", "localhost", proxyToPort.ToString(), requestTimeout);
            appBuilder.UseMiddleware<ConditionalProxyMiddleware>(publicPath, proxyOptions);
        }

#pragma warning disable CS0649
        class WebpackDevServerInfo
        {
            public int Port { get; set; }
            public string[] PublicPaths { get; set; }

            // For back-compatibility with older versions of aspnet-webpack, in the case where your webpack
            // configuration contains exactly one config entry. This will be removed soon.
            public string PublicPath { get; set; }
        }
    }
#pragma warning restore CS0649
}
