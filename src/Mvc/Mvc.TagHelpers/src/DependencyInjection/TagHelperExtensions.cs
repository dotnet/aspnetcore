// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring Razor cache tag helpers.
/// </summary>
public static class TagHelperServicesExtensions
{
    /// <summary>
    ///  Adds MVC cache tag helper services to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddCacheTagHelper(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddCacheTagHelperServices(builder.Services);

        return builder;
    }

    /// <summary>
    ///  Configures the memory size limits on the cache of the <see cref="CacheTagHelper"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="configure">The <see cref="Action{CacheTagHelperOptions}"/>to configure the cache options.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddCacheTagHelperLimits(this IMvcBuilder builder, Action<CacheTagHelperOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(configure);

        return builder;
    }

    /// <summary>
    ///  Configures the memory size limits on the cache of the <see cref="CacheTagHelper"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="configure">The <see cref="Action{CacheTagHelperOptions}"/>to configure the cache options.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddCacheTagHelperLimits(this IMvcCoreBuilder builder, Action<CacheTagHelperOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(configure);

        return builder;
    }

    internal static void AddCacheTagHelperServices(IServiceCollection services)
    {
        services.TryAddSingleton<IDistributedCacheTagHelperStorage, DistributedCacheTagHelperStorage>();
        services.TryAddSingleton<IDistributedCacheTagHelperFormatter, DistributedCacheTagHelperFormatter>();
        services.TryAddSingleton<IDistributedCacheTagHelperService, DistributedCacheTagHelperService>();

        // Required default services for cache tag helpers
        services.AddDistributedMemoryCache();
        services.TryAddSingleton<CacheTagHelperMemoryCacheFactory>();
    }
}
