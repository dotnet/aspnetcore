// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Metrics;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable RS0016 // Add public types and members to the declared API
internal static class MetricsServiceExtensions
{
    public static IServiceCollection AddMetrics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IMeterFactory, DefaultMeterFactory>();
        services.TryAddSingleton<IMeterRegistry, DefaultMeterRegistry>();

        return services;
    }

    public static IServiceCollection AddMetrics(this IServiceCollection services, Action<IMetricsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMetrics();
        configure(new MetricsBuilder(services));

        return services;
    }

    public static IMetricsBuilder AddDefaultTag(this IMetricsBuilder builder, string name, object? value)
    {
        builder.Services.Configure<MetricsOptions>(o => o.DefaultTags.Add(new KeyValuePair<string, object?>(name, value)));
        return builder;
    }
}
#pragma warning restore RS0016 // Add public types and members to the declared API
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
