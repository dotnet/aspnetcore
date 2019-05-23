// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    //
    // This code was taken virtually as-is from the Microsoft.Extensions.Hosting project in aspnet/Hosting and then
    // lots of things were removed.
    //
    internal class WebAssemblyHostBuilder : IWebAssemblyHostBuilder
    {
        private List<Action<WebAssemblyHostBuilderContext, IServiceCollection>> _configureServicesActions = new List<Action<WebAssemblyHostBuilderContext, IServiceCollection>>();
        private bool _hostBuilt;
        private WebAssemblyHostBuilderContext _BrowserHostBuilderContext;
        private IWebAssemblyServiceFactoryAdapter _serviceProviderFactory = new WebAssemblyServiceFactoryAdapter<IServiceCollection>(new DefaultServiceProviderFactory());
        private IServiceProvider _appServices;

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IWebAssemblyHostBuilder"/> for chaining.</returns>
        public IWebAssemblyHostBuilder ConfigureServices(Action<WebAssemblyHostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configureServicesActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <returns>The same instance of the <see cref="IWebAssemblyHostBuilder"/> for chaining.</returns>
        public IWebAssemblyHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            _serviceProviderFactory = new WebAssemblyServiceFactoryAdapter<TContainerBuilder>(factory ?? throw new ArgumentNullException(nameof(factory)));
            return this;
        }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <returns>The same instance of the <see cref="IWebAssemblyHostBuilder"/> for chaining.</returns>
        public IWebAssemblyHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<WebAssemblyHostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            _serviceProviderFactory = new WebAssemblyServiceFactoryAdapter<TContainerBuilder>(() => _BrowserHostBuilderContext, factory ?? throw new ArgumentNullException(nameof(factory)));
            return this;
        }

        /// <summary>
        /// Run the given actions to initialize the host. This can only be called once.
        /// </summary>
        /// <returns>An initialized <see cref="IWebAssemblyHost"/></returns>
        public IWebAssemblyHost Build()
        {
            if (_hostBuilt)
            {
                throw new InvalidOperationException("Build can only be called once.");
            }
            _hostBuilt = true;

            CreateBrowserHostBuilderContext();
            CreateServiceProvider();

            return _appServices.GetRequiredService<IWebAssemblyHost>();
        }

        private void CreateBrowserHostBuilderContext()
        {
            _BrowserHostBuilderContext = new WebAssemblyHostBuilderContext(Properties);
        }

        private void CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_BrowserHostBuilderContext);
            services.AddSingleton<IWebAssemblyHost, WebAssemblyHost>();
            services.AddSingleton<IJSRuntime>(WebAssemblyJSRuntime.Instance);
            services.AddSingleton<IComponentContext, WebAssemblyComponentContext>();
            services.AddSingleton<IUriHelper>(WebAssemblyUriHelper.Instance);
            services.AddSingleton<INavigationInterception>(WebAssemblyNavigationInterception.Instance);
            services.AddSingleton<HttpClient>(s =>
            {
                // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                var uriHelper = s.GetRequiredService<IUriHelper>();
                return new HttpClient
                {
                    BaseAddress = new Uri(WebAssemblyUriHelper.Instance.GetBaseUri())
                };
            });

            // Needed for authorization
            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(WebAssemblyConsoleLogger<>)));

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_BrowserHostBuilderContext, services);
            }

            var builder = _serviceProviderFactory.CreateBuilder(services);
            _appServices = _serviceProviderFactory.CreateServiceProvider(builder);
        }
    }
}
