// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Grpc.HttpApi;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal.Binding;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the gRPC HTTP API services.
/// </summary>
public static class GrpcHttpApiServiceExtensions
{
    /// <summary>
    /// Adds gRPC HTTP API services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddGrpcHttpApi(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddGrpc();
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IServiceMethodProvider<>), typeof(HttpApiServiceMethodProvider<>)));

        return services;
    }

    /// <summary>
    /// Adds gRPC HTTP API services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">An <see cref="Action{GrpcHttpApiOptions}"/> to configure the provided <see cref="GrpcHttpApiOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddGrpcHttpApi(this IServiceCollection services, Action<GrpcHttpApiOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        return services.Configure(configureOptions).AddGrpcHttpApi();
    }
}
