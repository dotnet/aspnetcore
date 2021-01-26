// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Provides an interface for initializing services and middleware used by an application.
    /// </summary>
    public interface IStartup
    {
        /// <summary>
        /// Register services into the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        IServiceProvider ConfigureServices(IServiceCollection services);

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">An <see cref="IApplicationBuilder"/> for the app to configure.</param>
        void Configure(IApplicationBuilder app);
    }
}
