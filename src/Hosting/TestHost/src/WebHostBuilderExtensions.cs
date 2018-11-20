// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.TestHost
{
    public static class WebHostBuilderExtensions
    {
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

            webHostBuilder.ConfigureServices(
                s => s.AddSingleton<IStartupConfigureServicesFilter>(
                    new ConfigureTestServicesStartupConfigureServicesFilter(servicesConfiguration)));

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

            webHostBuilder.ConfigureServices(
                s => s.AddSingleton<IStartupConfigureContainerFilter<TContainer>>(
                    new ConfigureTestServicesStartupConfigureContainerFilter<TContainer>(servicesConfiguration)));

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

        private class ConfigureTestServicesStartupConfigureServicesFilter : IStartupConfigureServicesFilter
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

        private class ConfigureTestServicesStartupConfigureContainerFilter<TContainer> : IStartupConfigureContainerFilter<TContainer>
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
