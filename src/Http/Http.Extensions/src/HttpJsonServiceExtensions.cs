// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure JSON serialization behavior.
/// </summary>
public static class HttpJsonServiceExtensions
{
    /// <summary>
    /// Configures options used for reading and writing JSON when using
    /// <see cref="O:Microsoft.AspNetCore.Http.HttpRequestJsonExtensions.ReadFromJsonAsync" />
    /// and <see cref="O:Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync" />.
    /// <see cref="JsonOptions"/> uses default values from <c>JsonSerializerDefaults.Web</c>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to configure options on.</param>
    /// <param name="configureOptions">The <see cref="Action{JsonOptions}"/> to configure the
    /// <see cref="JsonOptions"/>, uses default values from <c>JsonSerializerDefaults.Web</c>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection ConfigureHttpJsonOptions(this IServiceCollection services, Action<JsonOptions> configureOptions)
    {
        services.Configure<JsonOptions>(configureOptions);
        return services;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureDefaultHttpJsonOptions(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JsonOptions>, DefaultHttpJsonOptionsSetup>());
        return services;
    }
}
