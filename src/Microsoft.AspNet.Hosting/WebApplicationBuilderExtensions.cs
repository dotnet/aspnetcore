// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplicationBuilderExtensions
    {
        private static readonly string ServerUrlsSeparator = ";";

        public static IWebApplicationBuilder UseStartup<TStartup>(this IWebApplicationBuilder applicationBuilder) where TStartup : class
        {
            return applicationBuilder.UseStartup(typeof(TStartup));
        }

        public static IWebApplicationBuilder UseServer(this IWebApplicationBuilder applicationBuilder, string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            return applicationBuilder.UseSetting(WebApplicationConfiguration.ServerKey, assemblyName);
        }

        public static IWebApplicationBuilder UseServer(this IWebApplicationBuilder applicationBuilder, IServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            return applicationBuilder.UseServer(new ServerFactory(server));
        }

        public static IWebApplicationBuilder UseApplicationBasePath(this IWebApplicationBuilder applicationBuilder, string applicationBasePath)
        {
            if (applicationBasePath == null)
            {
                throw new ArgumentNullException(nameof(applicationBasePath));
            }

            return applicationBuilder.UseSetting(WebApplicationConfiguration.ApplicationBaseKey, applicationBasePath);
        }

        public static IWebApplicationBuilder UseEnvironment(this IWebApplicationBuilder applicationBuilder, string environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return applicationBuilder.UseSetting(WebApplicationConfiguration.EnvironmentKey, environment);
        }

        public static IWebApplicationBuilder UseWebRoot(this IWebApplicationBuilder applicationBuilder, string webRoot)
        {
            if (webRoot == null)
            {
                throw new ArgumentNullException(nameof(webRoot));
            }

            return applicationBuilder.UseSetting(WebApplicationConfiguration.WebRootKey, webRoot);
        }

        public static IWebApplicationBuilder UseUrls(this IWebApplicationBuilder applicationBuilder, params string[] urls)
        {
            if (urls == null)
            {
                throw new ArgumentNullException(nameof(urls));
            }

            return applicationBuilder.UseSetting(WebApplicationConfiguration.ServerUrlsKey, string.Join(ServerUrlsSeparator, urls));
        }

        public static IWebApplicationBuilder UseStartup(this IWebApplicationBuilder applicationBuilder, string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }

            return applicationBuilder.UseSetting(WebApplicationConfiguration.ApplicationKey, startupAssemblyName);
        }

        public static IWebApplication Start(this IWebApplicationBuilder applicationBuilder, params string[] urls)
        {
            var application = applicationBuilder.UseUrls(urls).Build();
            application.Start();
            return application;
        }

        /// <summary>
        /// Runs a web application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="application"></param>
        public static void Run(this IWebApplication application)
        {
            using (application)
            {
                application.Start();

                var hostingEnvironment = application.Services.GetService<IHostingEnvironment>();
                var applicationLifetime = application.Services.GetService<IApplicationLifetime>();

                Console.WriteLine("Hosting environment: " + hostingEnvironment.EnvironmentName);

                var serverAddresses = application.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
                if (serverAddresses != null)
                {
                    foreach (var address in serverAddresses)
                    {
                        Console.WriteLine("Now listening on: " + address);
                    }
                }

                Console.WriteLine("Application started. Press Ctrl+C to shut down.");

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    applicationLifetime.StopApplication();

                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                applicationLifetime.ApplicationStopping.WaitHandle.WaitOne();
            }
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