// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        private Func<IServiceProvider> _createServiceProvider;

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

            _createServiceProvider = () =>
            {
                return Services.BuildServiceProvider();
            };

            InitializeEnvironment();
        }

        private void InitializeEnvironment()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")))
            {
                // The remainder of this method relies on the ability to make .NET WebAssembly-specific JSInterop calls.
                return;
            }

            var applicationEnvironment = DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<string>("Blazor._internal.getApplicationEnvironment");
            Services.AddSingleton<IWebAssemblyHostEnvironment>(new WebAssemblyHostEnvironment(applicationEnvironment));

            var appSettingsJson = DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<string, byte[]>(
                "Blazor._internal.getConfig",
                "appsettings.json");
            if (appSettingsJson != null)
            {
                Configuration.AddJsonStream(new MemoryStream(appSettingsJson));
            }

            var appSettingsEnvironmentJson = DefaultWebAssemblyJSRuntime.Instance.InvokeUnmarshalled<string, byte[]>(
                "Blazor._internal.getConfig",
                $"appsettings.{applicationEnvironment}.json");

            if (appSettingsEnvironmentJson != null)
            {
                Configuration.AddJsonStream(new MemoryStream(appSettingsEnvironmentJson));
            }
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
        /// Registers a <see cref="IServiceProviderFactory{TBuilder}" /> instance to be used to create the <see cref="IServiceProvider" />.
        /// </summary>
        /// <param name="factory">The <see cref="IServiceProviderFactory{TBuilder}" />.</param>
        /// <param name="configure">
        /// A delegate used to configure the <typeparamref T="TBuilder" />. This can be used to configure services using
        /// APIS specific to the <see cref="IServiceProviderFactory{TBuilder}" /> implementation.
        /// </param>
        /// <typeparam name="TBuilder">The type of builder provided by the <see cref="IServiceProviderFactory{TBuilder}" />.</typeparam>
        /// <remarks>
        /// <para>
        /// <see cref="ConfigureContainer{TBuilder}(IServiceProviderFactory{TBuilder}, Action{TBuilder})"/> is called by <see cref="Build"/>
        /// and so the delegate provided by <paramref name="configure"/> will run after all other services have been registered. 
        /// </para>
        /// <para>
        /// Multiple calls to <see cref="ConfigureContainer{TBuilder}(IServiceProviderFactory{TBuilder}, Action{TBuilder})"/> will replace
        /// the previously stored <paramref name="factory"/> and <paramref name="configure"/> delegate. 
        /// </para>
        /// </remarks>
        public void ConfigureContainer<TBuilder>(IServiceProviderFactory<TBuilder> factory, Action<TBuilder> configure = null)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _createServiceProvider = () =>
            {
                var container = factory.CreateBuilder(Services);
                configure?.Invoke(container);
                return factory.CreateServiceProvider(container);
            };
        }

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
            var services = _createServiceProvider();
            var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();

            return new WebAssemblyHost(services, scope, configuration, RootComponents.ToArray());
        }

        internal void InitializeDefaultServices()
        {
            Services.AddSingleton<IJSRuntime>(DefaultWebAssemblyJSRuntime.Instance);
            Services.AddSingleton<NavigationManager>(WebAssemblyNavigationManager.Instance);
            Services.AddSingleton<INavigationInterception>(WebAssemblyNavigationInterception.Instance);
            Services.AddSingleton<ILoggerFactory, WebAssemblyLoggerFactory>();
            Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(WebAssemblyConsoleLogger<>)));
        }
    }
}
