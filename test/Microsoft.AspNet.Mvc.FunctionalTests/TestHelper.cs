// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using InlineConstraints;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
using Xunit;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public static class TestHelper
    {
        public static IServiceProvider CreateServices(string applicationWebSiteName)
        {
            var originalProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = originalProvider.GetService<IApplicationEnvironment>();

            // When an application executes in a regular context, the application base path points to the root
            // directory where the application is located, for example MvcSample.Web. However, when executing
            // an aplication as part of a test, the ApplicationBasePath of the IApplicationEnvironment points
            // to the root folder of the test project.
            // To compensate for this, we need to calculate the original path and override the application
            // environment value so that components like the view engine work properly in the context of the
            // test.
            var appBasePath = CalculateApplicationBasePath(appEnvironment, applicationWebSiteName);
            var services = new ServiceCollection();
            services.AddInstance(
                typeof(IApplicationEnvironment),
                new TestApplicationEnvironment(appEnvironment, appBasePath));

            // Injecting a custom assembly provider via configuration setting. It's not good enough to just
            // add the service directly since it's registered by MVC. 
            var providerType = CreateAssemblyProviderType(applicationWebSiteName);

            var configuration = new TestConfigurationProvider();
            configuration.Configuration.Set(
                typeof(IControllerAssemblyProvider).FullName,
                providerType.AssemblyQualifiedName);

            services.AddInstance(
                typeof(ITestConfigurationProvider),
                configuration);

            return services.BuildServiceProvider(originalProvider);
        }

        // Calculate the path relative to the application base path.
        public static string CalculateApplicationBasePath(IApplicationEnvironment appEnvironment,
                                                          string applicationWebSiteName)
        {
            // Mvc/test/Microsoft.AspNet.Mvc.FunctionalTests
            var appBase = appEnvironment.ApplicationBasePath;

            // Mvc/test
            var test = Path.GetDirectoryName(appBase);

            // Mvc/test/WebSites/applicationWebSiteName
            return Path.GetFullPath(Path.Combine(appBase, "..", "WebSites", applicationWebSiteName));
        }

        private static Type CreateAssemblyProviderType(string siteName)
        {
            // Creates a service type that will limit MVC to only the controllers in the test site.
            // We only want this to happen when running in proc.
            var assembly = Assembly.Load(new AssemblyName(siteName));

            var providerType = typeof(TestAssemblyProvider<>).MakeGenericType(assembly.GetExportedTypes()[0]);
            return providerType;
        }
    }
}