// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    /// <summary>
    /// Abstraction for configuring a Blazor browser-based application.
    /// </summary>
    public interface IWebAssemblyHostBuilder
    {
        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <returns>The same instance of the <see cref="IWebAssemblyHostBuilder"/> for chaining.</returns>
        IWebAssemblyHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory);

        /// <summary>
        /// Overrides the factory used to create the service provider.
        /// </summary>
        /// <returns>The same instance of the <see cref="IWebAssemblyHostBuilder"/> for chaining.</returns>
        IWebAssemblyHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<WebAssemblyHostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory);

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IServiceCollection"/> that will be used
        /// to construct the <see cref="IServiceProvider"/>.</param>
        /// <returns>The same instance of the <see cref="IWebAssemblyHostBuilder"/> for chaining.</returns>
        IWebAssemblyHostBuilder ConfigureServices(Action<WebAssemblyHostBuilderContext, IServiceCollection> configureDelegate);

        /// <summary>
        /// Run the given actions to initialize the host. This can only be called once.
        /// </summary>
        /// <returns>An initialized <see cref="IWebAssemblyHost"/></returns>
        IWebAssemblyHost Build();
    }
}
