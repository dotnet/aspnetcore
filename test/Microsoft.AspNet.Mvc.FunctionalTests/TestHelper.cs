// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public static class TestHelper
    {
        // Path from Mvc\\test\\Microsoft.AspNet.Mvc.FunctionalTests
        private static readonly string WebsitesDirectoryPath = Path.Combine("..", "WebSites");

        public static TestServer CreateServer(Action<IApplicationBuilder> builder, string applicationWebSiteName)
        {
            return CreateServer(builder, applicationWebSiteName, applicationPath: null);
        }

        public static TestServer CreateServer(
            Action<IApplicationBuilder> builder,
            string applicationWebSiteName,
            string applicationPath)
        {
            return CreateServer(
                builder,
                applicationWebSiteName,
                applicationPath,
                configureServices: (Action<IServiceCollection>)null);
        }

        public static TestServer CreateServer(
            Action<IApplicationBuilder> builder,
            string applicationWebSiteName,
            Action<IServiceCollection> configureServices)
        {
            return CreateServer(
                builder,
                applicationWebSiteName,
                applicationPath: null,
                configureServices: configureServices);
        }

        public static TestServer CreateServer(
            Action<IApplicationBuilder> builder,
            string applicationWebSiteName,
            string applicationPath,
            Action<IServiceCollection> configureServices)
        {
            return TestServer.Create(
                builder,
                services => AddTestServices(services, applicationWebSiteName, applicationPath, configureServices));
        }

        public static TestServer CreateServer(
            Action<IApplicationBuilder> builder,
            string applicationWebSiteName,
            Func<IServiceCollection, IServiceProvider> configureServices)
        {
            return CreateServer(
                builder,
                applicationWebSiteName,
                applicationPath: null,
                configureServices: configureServices);
        }

        public static TestServer CreateServer(
            Action<IApplicationBuilder> builder,
            string applicationWebSiteName,
            string applicationPath,
            Func<IServiceCollection, IServiceProvider> configureServices)
        {
            return TestServer.Create(
                CallContextServiceLocator.Locator.ServiceProvider,
                builder,
                services =>
                {
                    AddTestServices(services, applicationWebSiteName, applicationPath, configureServices: null);
                    return (configureServices != null) ? configureServices(services) : services.BuildServiceProvider();
                });
        }

        private static void AddTestServices(
            IServiceCollection services,
            string applicationWebSiteName,
            string applicationPath,
            Action<IServiceCollection> configureServices)
        {
            applicationPath = applicationPath ?? WebsitesDirectoryPath;

            // Get current IApplicationEnvironment; likely added by the host.
            var provider = services.BuildServiceProvider();
            var originalEnvironment = provider.GetRequiredService<IApplicationEnvironment>();

            // When an application executes in a regular context, the application base path points to the root
            // directory where the application is located, for example MvcSample.Web. However, when executing
            // an application as part of a test, the ApplicationBasePath of the IApplicationEnvironment points
            // to the root folder of the test project.
            // To compensate for this, we need to calculate the original path and override the application
            // environment value so that components like the view engine work properly in the context of the
            // test.
            var applicationBasePath = CalculateApplicationBasePath(
                originalEnvironment,
                applicationWebSiteName,
                applicationPath);
            var environment = new TestApplicationEnvironment(
                originalEnvironment,
                applicationBasePath,
                applicationWebSiteName);
            services.AddInstance<IApplicationEnvironment>(environment);
            var hostingEnvironment = new HostingEnvironment();
            hostingEnvironment.Initialize(applicationBasePath, environmentName: null);
            services.AddInstance<IHostingEnvironment>(hostingEnvironment);

            // Injecting a custom assembly provider. Overrides AddMvc() because that uses TryAdd().
            var assemblyProvider = CreateAssemblyProvider(applicationWebSiteName);
            services.AddInstance(assemblyProvider);

            if (configureServices != null)
            {
                configureServices(services);
            }
        }

        // Calculate the path relative to the application base path.
        private static string CalculateApplicationBasePath(
            IApplicationEnvironment appEnvironment,
            string applicationWebSiteName,
            string websitePath)
        {
            // Mvc/test/WebSites/applicationWebSiteName
            return Path.GetFullPath(
                Path.Combine(appEnvironment.ApplicationBasePath, websitePath, applicationWebSiteName));
        }

        private static IAssemblyProvider CreateAssemblyProvider(string siteName)
        {
            // Creates a service type that will limit MVC to only the controllers in the test site.
            // We only want this to happen when running in-process.
            var assembly = Assembly.Load(new AssemblyName(siteName));
            var provider = new FixedSetAssemblyProvider
            {
                CandidateAssemblies =
                {
                    assembly,
                },
            };

            return provider;
        }
    }
}