// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupMethods
    {
        internal static Func<IServiceCollection, IServiceProvider> DefaultBuildServiceProvider = s => s.BuildServiceProvider();

        public StartupMethods(Action<IApplicationBuilder> configure)
            : this(configure, configureServices: null)
        {
        }

        public StartupMethods(Action<IApplicationBuilder> configure, Func<IServiceCollection, IServiceProvider> configureServices)
        {
            ConfigureDelegate = configure;
            ConfigureServicesDelegate = configureServices ?? DefaultBuildServiceProvider;
        }

        public Func<IServiceCollection, IServiceProvider> ConfigureServicesDelegate { get; }
        public Action<IApplicationBuilder> ConfigureDelegate { get; }

    }
}