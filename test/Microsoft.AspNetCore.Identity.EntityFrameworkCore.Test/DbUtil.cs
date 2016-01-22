// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    public static class DbUtil
    {
        public static IServiceCollection ConfigureDbServices(string connectionString, IServiceCollection services = null)
        {
            return ConfigureDbServices<IdentityDbContext>(connectionString, services);
        }

        public static IServiceCollection ConfigureDbServices<TContext>(string connectionString, IServiceCollection services = null) where TContext : DbContext
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddEntityFramework().AddSqlServer().AddDbContext<TContext>(options => options.UseSqlServer(connectionString));
            return services;
        }

        public static TContext Create<TContext>(string connectionString) where TContext : DbContext, new()
        {
            var serviceProvider = ConfigureDbServices<TContext>(connectionString).BuildServiceProvider();
            return serviceProvider.GetRequiredService<TContext>();
        }
    }
}