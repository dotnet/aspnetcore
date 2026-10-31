// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection;

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
        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new EntityFrameworkCoreXmlRepository<TContext>(services, loggerFactory);
            });
        });

        return builder;
    }
}
