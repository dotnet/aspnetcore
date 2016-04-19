// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class MvcTestFixture<TStartup> : IDisposable
    {
        private readonly TestServer _server;

        public MvcTestFixture()
#if NET451
            : this(Path.Combine("..", "..", "..", "..", "..", "WebSites"))
#else
            : this(Path.Combine("..", "..", "..", "..", "WebSites"))
#endif
        {
        }

        protected MvcTestFixture(string relativePath)
        {
            // RequestLocalizationOptions saves the current culture when constructed, potentially changing response
            // localization i.e. RequestLocalizationMiddleware behavior. Ensure the saved culture
            // (DefaultRequestCulture) is consistent regardless of system configuration or personal preferences.
            using (new CultureReplacer())
            {
                var builder = new WebHostBuilder()
                    .UseContentRoot(GetApplicationPath(relativePath))
                    .ConfigureServices(InitializeServices)
                    .UseStartup(typeof(TStartup));

                _server = new TestServer(builder);
            }

            Client = _server.CreateClient();
            Client.BaseAddress = new Uri("http://localhost");
        }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }

        private static string GetApplicationPath(string relativePath)
        {
            var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;
            var applicationName = startupAssembly.GetName().Name;
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;
            return Path.GetFullPath(Path.Combine(applicationBasePath, relativePath, applicationName));
        }

        protected virtual void InitializeServices(IServiceCollection services)
        {
            var startupAssembly = typeof(TStartup).GetTypeInfo().Assembly;

            // Inject a custom application part manager. Overrides AddMvcCore() because that uses TryAdd().
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new AssemblyPart(startupAssembly));

            manager.FeatureProviders.Add(new ControllerFeatureProvider());
            manager.FeatureProviders.Add(new ViewComponentFeatureProvider());

            services.AddSingleton(manager);
        }
    }
}
