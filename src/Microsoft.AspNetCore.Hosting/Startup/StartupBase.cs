// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public abstract class StartupBase : IStartup
    {
        public abstract void Configure(IApplicationBuilder app);

        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider();
        }
    }

    public abstract class StartupBase<TContainerBuilder> : IStartup
    {
        private readonly IServiceProviderFactory<TContainerBuilder> _factory;

        public StartupBase(IServiceProviderFactory<TContainerBuilder> factory)
        {
            _factory = factory;
        }

        public abstract void Configure(IApplicationBuilder app);

        public virtual void ConfigureServices(IServiceCollection services)
        {

        }

        IServiceProvider IStartup.ConfigureServices(IServiceCollection services)
        {
            ConfigureServices(services);
            var builder = _factory.CreateBuilder(services);
            ConfigureContainer(builder);
            return _factory.CreateServiceProvider(builder);
        }

        public virtual void ConfigureContainer(TContainerBuilder containerBuilder) { }
    }
}