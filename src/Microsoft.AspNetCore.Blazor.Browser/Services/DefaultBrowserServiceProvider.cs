// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.Blazor.Browser.Services
{
    /// <summary>
    /// An implementation of <see cref="IServiceProvider"/> configured with
    /// default services suitable for use in a browser environment.
    /// </summary>
    public class DefaultBrowserServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _underlyingProvider;

        /// <summary>
        /// Constructs an instance of <see cref="DefaultBrowserServiceProvider"/>.
        /// </summary>
        public DefaultBrowserServiceProvider(): this(null)
        {
        }

        /// <summary>
        /// Constructs an instance of <see cref="DefaultBrowserServiceProvider"/>.
        /// </summary>
        /// <param name="configure">A callback that can be used to configure the <see cref="IServiceCollection"/>.</param>
        public DefaultBrowserServiceProvider(Action<IServiceCollection> configure)
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
            // TODO: Add default services for HttpClient, IUrlHelper, etc.
        }
    }
}
