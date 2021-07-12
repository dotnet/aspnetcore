// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Used for initializing services and middlewares used by an application.
    /// </summary>
    public class DelegateStartup : StartupBase<IServiceCollection>
    {
        private readonly Action<IApplicationBuilder> _configureApp;

        /// <summary>
        /// Creates a new <see cref="DelegateStartup" /> instance.
        /// </summary>
        /// <param name="factory">A factory for creating <see cref="IServiceProvider"/> instances.</param>
        /// <param name="configureApp">An <see cref="Action"/> for configuring the application.</param>
        public DelegateStartup(IServiceProviderFactory<IServiceCollection> factory, Action<IApplicationBuilder> configureApp) : base(factory)
        {
            _configureApp = configureApp;
        }

        /// <summary>
        /// Configures the <see cref="IApplicationBuilder"/> with the initialized <see cref="Action"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        public override void Configure(IApplicationBuilder app) => _configureApp(app);
    }
}
