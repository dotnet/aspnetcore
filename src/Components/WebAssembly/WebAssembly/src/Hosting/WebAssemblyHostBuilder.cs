// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// A builder for configuring and creating a <see cref="WebAssemblyHost"/>.
    /// </summary>
    public sealed class WebAssemblyHostBuilder
    {
        /// <summary>
        /// Creates an instance of <see cref="WebAssemblyHostBuilder"/> using the most common
        /// conventions and settings.
        /// </summary>
        /// <param name="args">The argument passed to the application's main method.</param>
        /// <returns>A <see cref="WebAssemblyHostBuilder"/>.</returns>
        public static WebAssemblyHostBuilder CreateDefault(string[] args = default)
        {
            // We don't use the args for anything right now, but we want to accept them
            // here so that it shows up this way in the project templates.
            args ??= Array.Empty<string>();
            var builder = new WebAssemblyHostBuilder();

            // Right now we don't have conventions or behaviors that are specific to this method
            // however, making this the default for the template allows us to add things like that
            // in the future, while giving `new WebAssemblyHostBuilder` as an opt-out of opinionated
            // settings.
            return builder;
        }

        /// <summary>
        /// Creates an instance of <see cref="WebAssemblyHostBuilder"/> with the minimal configuration.
        /// </summary>
        private WebAssemblyHostBuilder()
        {
            // Private right now because we don't have much reason to expose it. This can be exposed
            // in the future if we want to give people a choice between CreateDefault and something
            // less opinionated.
            Configuration = new ConfigurationBuilder();
            RootComponents = new RootComponentMappingCollection();
            Services = new ServiceCollection();

            InitializeDefaultServices();
        }

        /// <summary>
        /// Gets an <see cref="IConfigurationBuilder"/> that can be used to customize the application's
        /// configuration sources.
        /// </summary>
        public IConfigurationBuilder Configuration { get; }

        /// <summary>
        /// Gets the collection of root component mappings configured for the application.
        /// </summary>
        public RootComponentMappingCollection RootComponents { get; }

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Builds a <see cref="WebAssemblyHost"/> instance based on the configuration of this builder.
        /// </summary>
        /// <returns>A <see cref="WebAssemblyHost"/> object.</returns>
        public WebAssemblyHost Build()
        {
            // Intentionally overwrite configuration with the one we're creating.
            var configuration = Configuration.Build();
            Services.AddSingleton<IConfiguration>(configuration);

            // A Blazor application always runs in a scope. Since we want to make it possible for the user
            // to configure services inside *that scope* inside their startup code, we create *both* the
            // service provider and the scope here.
            var services = Services.BuildServiceProvider();
            var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();

            return new WebAssemblyHost(services, scope, configuration, RootComponents.ToArray());
        }

        private void InitializeDefaultServices()
        {
            Services.AddSingleton<IJSRuntime>(WebAssemblyJSRuntime.Instance);
            Services.AddSingleton<NavigationManager>(WebAssemblyNavigationManager.Instance);
            Services.AddSingleton<INavigationInterception>(WebAssemblyNavigationInterception.Instance);
            Services.AddSingleton<ILoggerFactory, WebAssemblyLoggerFactory>();
            Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(WebAssemblyConsoleLogger<>)));
            Services.AddSingleton<HttpClient>(s =>
            {
                // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                var navigationManager = s.GetRequiredService<NavigationManager>();
                return new HttpClient
                {
                    BaseAddress = new Uri(navigationManager.BaseUri)
                };
            });
        }
    }
}
