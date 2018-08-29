// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
{
    /// <summary>
    /// Extension method class for configuring instances of <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>
    /// </summary>
    public static class EntityFrameworkCoreDataProtectionExtensions
    {
        /// <summary>
        /// Configures the data protection system to persist keys to an EntityFrameworkCore datastore
        /// </summary>
        /// <param name="builder">The <see cref="IDataProtectionBuilder"/> instance to modify.</param>
        /// <returns>The value <paramref name="builder"/>.</returns>
        public static IDataProtectionBuilder PersistKeysToDbContext<TContext>(this IDataProtectionBuilder builder)
            where TContext : DbContext, IDataProtectionKeyContext
        {
            var services = builder.Services;

            services.AddScoped<Func<TContext>>(
                provider => new Func<TContext>(
                    () => provider.CreateScope().ServiceProvider.GetService<TContext>()));

            services.AddScoped<IXmlRepository>(provider =>
            {
                var scope = provider.CreateScope();
                return new EntityFrameworkCoreXmlRepository<TContext>(
                    contextFactory: scope.ServiceProvider.GetRequiredService<Func<TContext>>(),
                    loggerFactory: scope.ServiceProvider.GetService<ILoggerFactory>());
            });

            services.AddTransient<IConfigureOptions<KeyManagementOptions>, ConfigureKeyManagementOptions>();

            return builder;
        }
    }
}
