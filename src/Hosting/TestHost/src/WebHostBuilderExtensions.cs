// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.TestHost
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseTestServer(this IWebHostBuilder builder)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddSingleton<IServer, TestServer>();
            });
        }

        /// <summary>
        /// Retrieves the TestServer from the host services.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static TestServer GetTestServer(this IWebHost host)
        {
            return (TestServer)host.Services.GetRequiredService<IServer>();
        }

        /// <summary>
        /// Retrieves the test client from the TestServer in the host services.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static HttpClient GetTestClient(this IWebHost host)
        {
            return host.GetTestServer().CreateClient();
        }

        public static IWebHostBuilder ConfigureTestServices(this IWebHostBuilder webHostBuilder, Action<IServiceCollection> servicesConfiguration)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            if (servicesConfiguration == null)
            {
                throw new ArgumentNullException(nameof(servicesConfiguration));
            }

            if (webHostBuilder.GetType().Name.Equals("GenericWebHostBuilder"))
            {
                // Generic host doesn't need to do anything special here since there's only one container.
                webHostBuilder.ConfigureServices(servicesConfiguration);
            }
            else
            {
#pragma warning disable CS0612 // Type or member is obsolete
                webHostBuilder.ConfigureServices(
                    s => s.AddSingleton<IStartupConfigureServicesFilter>(
                        new ConfigureTestServicesStartupConfigureServicesFilter(servicesConfiguration)));
#pragma warning restore CS0612 // Type or member is obsolete
            }

            return webHostBuilder;
        }

        public static IWebHostBuilder ConfigureTestContainer<TContainer>(this IWebHostBuilder webHostBuilder, Action<TContainer> servicesConfiguration)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            if (servicesConfiguration == null)
            {
                throw new ArgumentNullException(nameof(servicesConfiguration));
            }

#pragma warning disable CS0612 // Type or member is obsolete
            webHostBuilder.ConfigureServices(
                s => s.AddSingleton<IStartupConfigureContainerFilter<TContainer>>(
                    new ConfigureTestServicesStartupConfigureContainerFilter<TContainer>(servicesConfiguration)));
#pragma warning restore CS0612 // Type or member is obsolete

            return webHostBuilder;
        }

        public static IWebHostBuilder UseSolutionRelativeContentRoot(
            this IWebHostBuilder builder,
            string solutionRelativePath,
            string solutionName = "*.sln")
        {
            return builder.UseSolutionRelativeContentRoot(solutionRelativePath, AppContext.BaseDirectory, solutionName);
        }

        public static IWebHostBuilder UseSolutionRelativeContentRoot(
            this IWebHostBuilder builder,
            string solutionRelativePath,
            string applicationBasePath,
            string solutionName = "*.sln")
        {
            if (solutionRelativePath == null)
            {
                throw new ArgumentNullException(nameof(solutionRelativePath));
            }

            if (applicationBasePath == null)
            {
                throw new ArgumentNullException(nameof(applicationBasePath));
            }

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionPath = Directory.EnumerateFiles(directoryInfo.FullName, solutionName).FirstOrDefault();
                if (solutionPath != null)
                {
                    builder.UseContentRoot(Path.GetFullPath(Path.Combine(directoryInfo.FullName, solutionRelativePath)));
                    return builder;
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new InvalidOperationException($"Solution root could not be located using application root {applicationBasePath}.");
        }

#pragma warning disable CS0612 // Type or member is obsolete
        private class ConfigureTestServicesStartupConfigureServicesFilter : IStartupConfigureServicesFilter
#pragma warning restore CS0612 // Type or member is obsolete
        {
            private readonly Action<IServiceCollection> _servicesConfiguration;

            public ConfigureTestServicesStartupConfigureServicesFilter(Action<IServiceCollection> servicesConfiguration)
            {
                if (servicesConfiguration == null)
                {
                    throw new ArgumentNullException(nameof(servicesConfiguration));
                }

                _servicesConfiguration = servicesConfiguration;
            }

            public Action<IServiceCollection> ConfigureServices(Action<IServiceCollection> next) =>
                serviceCollection =>
                {
                    next(serviceCollection);
                    _servicesConfiguration(serviceCollection);
                };
        }

#pragma warning disable CS0612 // Type or member is obsolete
        private class ConfigureTestServicesStartupConfigureContainerFilter<TContainer> : IStartupConfigureContainerFilter<TContainer>
#pragma warning restore CS0612 // Type or member is obsolete
        {
            private readonly Action<TContainer> _servicesConfiguration;

            public ConfigureTestServicesStartupConfigureContainerFilter(Action<TContainer> containerConfiguration)
            {
                if (containerConfiguration == null)
                {
                    throw new ArgumentNullException(nameof(containerConfiguration));
                }

                _servicesConfiguration = containerConfiguration;
            }

            public Action<TContainer> ConfigureContainer(Action<TContainer> next) =>
                containerBuilder =>
                {
                    next(containerBuilder);
                    _servicesConfiguration(containerBuilder);
                };
        }
    }
}
