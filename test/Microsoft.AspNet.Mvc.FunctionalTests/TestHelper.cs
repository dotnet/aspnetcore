// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public static class TestHelper
    {
        // Path from Mvc\\test\\Microsoft.AspNet.Mvc.FunctionalTests
        private static readonly string WebsitesDirectoryPath = Path.Combine("..", "WebSites");

        public static IServiceProvider CreateServices(string applicationWebSiteName, IServiceCollection newServices = null)
        {
            return CreateServices(applicationWebSiteName, WebsitesDirectoryPath, newServices);
        }

        public static IServiceProvider CreateServices(string applicationWebSiteName, string applicationPath, IServiceCollection newServices = null)
        {
            var originalProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = originalProvider.GetRequiredService<IApplicationEnvironment>();

            // When an application executes in a regular context, the application base path points to the root
            // directory where the application is located, for example MvcSample.Web. However, when executing
            // an aplication as part of a test, the ApplicationBasePath of the IApplicationEnvironment points
            // to the root folder of the test project.
            // To compensate for this, we need to calculate the original path and override the application
            // environment value so that components like the view engine work properly in the context of the
            // test.
            var appBasePath = CalculateApplicationBasePath(appEnvironment, applicationWebSiteName, applicationPath);

            var services = new ServiceCollection();
            services.AddInstance(
                typeof(IApplicationEnvironment),
                new TestApplicationEnvironment(appEnvironment, appBasePath));

            // Injecting a custom assembly provider via configuration setting. It's not good enough to just
            // add the service directly since it's registered by MVC.
            var providerType = CreateAssemblyProviderType(applicationWebSiteName);

            var configuration = new TestConfigurationProvider();
            configuration.Configuration.Set(
                typeof(IAssemblyProvider).FullName,
                providerType.AssemblyQualifiedName);

            services.AddInstance(
                typeof(ITestConfigurationProvider),
                configuration);

            services.AddInstance(
                typeof(ILoggerFactory),
                NullLoggerFactory.Instance);

            if (newServices != null)
            {
                services.Add(newServices);
            }

            return BuildFallbackServiceProvider(services, originalProvider);
        }

        // Calculate the path relative to the application base path.
        public static string CalculateApplicationBasePath(IApplicationEnvironment appEnvironment,
                                                          string applicationWebSiteName, string websitePath)
        {
            // Mvc/test/WebSites/applicationWebSiteName
            return Path.GetFullPath(
                Path.Combine(appEnvironment.ApplicationBasePath, websitePath, applicationWebSiteName));
        }

        /// <summary>
        /// Creates a disposable action that replaces the service provider <see cref="CallContextServiceLocator.Locator"/>
        /// with the passed in service that is switched back on <see cref="IDisposable.Dispose"/>.
        /// </summary>
        /// <remarks>This is required for config since it uses the static property to get to
        /// <see cref="IApplicationEnvironment"/>.</remarks>
        public static IDisposable ReplaceCallContextServiceLocationService(IServiceProvider serviceProvider)
        {
            return new CallContextProviderAction(serviceProvider);
        }

        private static Type CreateAssemblyProviderType(string siteName)
        {
            // Creates a service type that will limit MVC to only the controllers in the test site.
            // We only want this to happen when running in proc.
            var assembly = Assembly.Load(new AssemblyName(siteName));

            var providerType = typeof(TestAssemblyProvider<>).MakeGenericType(assembly.GetExportedTypes()[0]);
            return providerType;
        }

        private sealed class CallContextProviderAction : IDisposable
        {
            private readonly IServiceProvider _originalProvider;

            public CallContextProviderAction(IServiceProvider provider)
            {
                _originalProvider = CallContextServiceLocator.Locator.ServiceProvider;
                CallContextServiceLocator.Locator.ServiceProvider = provider;
            }

            public void Dispose()
            {
                CallContextServiceLocator.Locator.ServiceProvider = _originalProvider;
            }
        }

        // TODO: Kill this code
        private static IServiceProvider BuildFallbackServiceProvider(IEnumerable<IServiceDescriptor> services, IServiceProvider fallback)
        {
            var sc = HostingServices.Create(fallback);
            sc.Add(services);

            // Build the manifest
            var manifestTypes = services.Where(t => t.ServiceType.GetTypeInfo().GenericTypeParameters.Length == 0
                    && t.ServiceType != typeof(IServiceManifest)
                    && t.ServiceType != typeof(IServiceProvider))
                    .Select(t => t.ServiceType).Distinct();
            sc.AddInstance<IServiceManifest>(new ServiceManifest(manifestTypes, fallback.GetRequiredService<IServiceManifest>()));
            return sc.BuildServiceProvider();
        }

        private class ServiceManifest : IServiceManifest
        {
            public ServiceManifest(IEnumerable<Type> services, IServiceManifest fallback = null)
            {
                Services = services;
                if (fallback != null)
                {
                    Services = Services.Concat(fallback.Services).Distinct();
                }
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}