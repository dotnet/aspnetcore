// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Http;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Microsoft.AspNetCore.Blazor.Browser.Services
{
    /// <summary>
    /// An implementation of <see cref="IServiceProvider"/> configured with
    /// default services suitable for use in a browser environment.
    /// </summary>
    public class BrowserServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _underlyingProvider;

        /// <summary>
        /// Constructs an instance of <see cref="BrowserServiceProvider"/>.
        /// </summary>
        public BrowserServiceProvider(): this(null)
        {
        }

        /// <summary>
        /// Constructs an instance of <see cref="BrowserServiceProvider"/>.
        /// </summary>
        /// <param name="configure">A callback that can be used to configure the <see cref="IServiceCollection"/>.</param>
        public BrowserServiceProvider(Action<IServiceCollection> configure)
        {
            var serviceCollection = new ServiceCollection();
            AddDefaultServices(serviceCollection);
            configure?.Invoke(serviceCollection);
            _underlyingProvider = serviceCollection.BuildServiceProvider();
        }

        /// <inheritdoc />
        public object GetService(Type serviceType)
            => _underlyingProvider.GetService(serviceType);

        private void AddDefaultServices(ServiceCollection serviceCollection)
        {
            var uriHelper = new BrowserUriHelper();
            serviceCollection.AddSingleton<IUriHelper>(uriHelper);
            serviceCollection.AddSingleton(new HttpClient(new BrowserHttpMessageHandler())
            {
                BaseAddress = new Uri(uriHelper.GetBaseUri())
            });
        }
    }
}
