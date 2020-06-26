// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
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
            var builder = new WebAssemblyHostBuilder(WebAssemblyJSRuntimeInvoker.Instance);

            // Right now we don't have conventions or behaviors that are specific to this method
            // however, making this the default for the template allows us to add things like that
            // in the future, while giving `new WebAssemblyHostBuilder` as an opt-out of opinionated
            // settings.
            return builder;
        }

        /// <summary>
        /// Creates an instance of <see cref="WebAssemblyHostBuilder"/> with the minimal configuration.
        /// </summary>
        internal WebAssemblyHostBuilder(WebAssemblyJSRuntimeInvoker jsRuntimeInvoker)
        {
            // Private right now because we don't have much reason to expose it. This can be exposed
            // in the future if we want to give people a choice between CreateDefault and something
            // less opinionated.
            Configuration = new WebAssemblyHostConfiguration();
            RootComponents = new RootComponentMappingCollection();
            Services = new ServiceCollection();
            Logging = new LoggingBuilder(Services);

            // Retrieve required attributes from JSRuntimeInvoker
            InitializeNavigationManager(jsRuntimeInvoker);
            InitializeDefaultServices();

            var hostEnvironment = InitializeEnvironment(jsRuntimeInvoker);
            HostEnvironment = hostEnvironment;

            _createServiceProvider = () =>
            {
                return Services.BuildServiceProvider(validateScopes: WebAssemblyHostEnvironmentExtensions.IsDevelopment(hostEnvironment));
            };
        }

        private void InitializeNavigationManager(WebAssemblyJSRuntimeInvoker jsRuntimeInvoker)
        {
            var baseUri = jsRuntimeInvoker.InvokeUnmarshalled<object, object, object, string>(BrowserNavigationManagerInterop.GetBaseUri, null, null, null);
            var uri = jsRuntimeInvoker.InvokeUnmarshalled<object, object, object, string>(BrowserNavigationManagerInterop.GetLocationHref, null, null, null);

            WebAssemblyNavigationManager.Instance = new WebAssemblyNavigationManager(baseUri, uri);
        }

        private WebAssemblyHostEnvironment InitializeEnvironment(WebAssemblyJSRuntimeInvoker jsRuntimeInvoker)
        {
            var applicationEnvironment = jsRuntimeInvoker.InvokeUnmarshalled<object, object, object, string>(
                "Blazor._internal.getApplicationEnvironment", null, null, null);
            var hostEnvironment = new WebAssemblyHostEnvironment(applicationEnvironment, WebAssemblyNavigationManager.Instance.BaseUri);

            Services.AddSingleton<IWebAssemblyHostEnvironment>(hostEnvironment);

            var configFiles = new[]
            {
                "appsettings.json",
                $"appsettings.{applicationEnvironment}.json"
            };

            foreach (var configFile in configFiles)
            {
                var appSettingsJson = jsRuntimeInvoker.InvokeUnmarshalled<string, object, object, byte[]>(
                    "Blazor._internal.getConfig", configFile, null, null);

                if (appSettingsJson != null)
                {
                    // Perf: Using this over AddJsonStream. This allows the linker to trim out the "File"-specific APIs and assemblies
                    // for Configuration, of where there are several.
                    Configuration.Add<JsonStreamConfigurationSource>(s => s.Stream = new MemoryStream(appSettingsJson));
                }
            }

            return hostEnvironment;
        }

        /// <summary>
        /// Gets an <see cref="WebAssemblyHostConfiguration"/> that can be used to customize the application's
        /// configuration sources and read configuration attributes.
        /// </summary>
        public WebAssemblyHostConfiguration Configuration { get; }

        /// <summary>
        /// Gets the collection of root component mappings configured for the application.
        /// </summary>
        public RootComponentMappingCollection RootComponents { get; }

        /// <summary>
        /// Gets the service collection.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Gets information about the app's host environment.
        /// </summary>
        public IWebAssemblyHostEnvironment HostEnvironment { get; }

        /// <summary>
        /// Gets the logging builder for configuring logging services.
        /// </summary>
        public ILoggingBuilder Logging { get;  }

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
            Services.AddSingleton<IConfiguration>(Configuration);

            // A Blazor application always runs in a scope. Since we want to make it possible for the user
            // to configure services inside *that scope* inside their startup code, we create *both* the
            // service provider and the scope here.
            var services = _createServiceProvider();
            var scope = services.GetRequiredService<IServiceScopeFactory>().CreateScope();

            return new WebAssemblyHost(services, scope, Configuration, RootComponents.ToArray());
        }

        internal void InitializeDefaultServices()
        {
            Services.AddSingleton<IJSRuntime>(DefaultWebAssemblyJSRuntime.Instance);
            Services.AddSingleton<NavigationManager>(WebAssemblyNavigationManager.Instance);
            Services.AddSingleton<INavigationInterception>(WebAssemblyNavigationInterception.Instance);
            Services.AddSingleton<WebAssemblyDynamicResourceLoader>(new WebAssemblyDynamicResourceLoader(DefaultWebAssemblyJSRuntime.Instance));
            Services.AddLogging(builder => {
                builder.AddProvider(new WebAssemblyConsoleLoggerProvider(DefaultWebAssemblyJSRuntime.Instance));
            });
        }
    }
}
