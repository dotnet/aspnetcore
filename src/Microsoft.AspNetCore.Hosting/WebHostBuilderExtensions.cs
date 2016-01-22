// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderExtensions
    {
        private static readonly string ServerUrlsSeparator = ";";

        public static IWebHostBuilder UseDefaultConfiguration(this IWebHostBuilder builder)
        {
            return builder.UseDefaultConfiguration(args: null);
        }

        public static IWebHostBuilder UseDefaultConfiguration(this IWebHostBuilder builder, string[] args)
        {
            return builder.UseConfiguration(WebHostConfiguration.GetDefault(args));
        }

        public static IWebHostBuilder UseCaptureStartupErrors(this IWebHostBuilder hostBuilder, bool captureStartupError)
        {
            return hostBuilder.UseSetting(WebHostDefaults.CaptureStartupErrorsKey, captureStartupError ? "true" : "false");
        }

        public static IWebHostBuilder UseStartup<TStartup>(this IWebHostBuilder hostBuilder) where TStartup : class
        {
            return hostBuilder.UseStartup(typeof(TStartup));
        }

        public static IWebHostBuilder UseServer(this IWebHostBuilder hostBuilder, string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            return hostBuilder.UseSetting(WebHostDefaults.ServerKey, assemblyName);
        }

        public static IWebHostBuilder UseServer(this IWebHostBuilder hostBuilder, IServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            return hostBuilder.UseServer(new ServerFactory(server));
        }

        public static IWebHostBuilder UseApplicationBasePath(this IWebHostBuilder hostBuilder, string applicationBasePath)
        {
            if (applicationBasePath == null)
            {
                throw new ArgumentNullException(nameof(applicationBasePath));
            }

            return hostBuilder.UseSetting(WebHostDefaults.ApplicationBaseKey, applicationBasePath);
        }

        public static IWebHostBuilder UseEnvironment(this IWebHostBuilder hostBuilder, string environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return hostBuilder.UseSetting(WebHostDefaults.EnvironmentKey, environment);
        }

        public static IWebHostBuilder UseWebRoot(this IWebHostBuilder hostBuilder, string webRoot)
        {
            if (webRoot == null)
            {
                throw new ArgumentNullException(nameof(webRoot));
            }

            return hostBuilder.UseSetting(WebHostDefaults.WebRootKey, webRoot);
        }

        public static IWebHostBuilder UseUrls(this IWebHostBuilder hostBuilder, params string[] urls)
        {
            if (urls == null)
            {
                throw new ArgumentNullException(nameof(urls));
            }

            return hostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(ServerUrlsSeparator, urls));
        }

        public static IWebHostBuilder UseStartup(this IWebHostBuilder hostBuilder, string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }

            return hostBuilder.UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName);
        }

        public static IWebHost Start(this IWebHostBuilder hostBuilder, params string[] urls)
        {
            var host = hostBuilder.UseUrls(urls).Build();
            host.Start();
            return host;
        }

        private class ServerFactory : IServerFactory
        {
            private readonly IServer _server;

            public ServerFactory(IServer server)
            {
                _server = server;
            }

            public IServer CreateServer(IConfiguration configuration) => _server;
        }
    }
}