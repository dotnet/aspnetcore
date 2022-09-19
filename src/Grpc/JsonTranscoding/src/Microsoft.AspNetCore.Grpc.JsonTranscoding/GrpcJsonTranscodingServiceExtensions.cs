// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Grpc.JsonTranscoding;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Binding;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the gRPC JSON transcoding services.
/// </summary>
public static class GrpcJsonTranscodingServiceExtensions
{
    /// <summary>
    /// Adds gRPC JSON transcoding services to the specified <see cref="IGrpcServerBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="IGrpcServerBuilder"/>.</param>
    /// <returns>The same instance of the <see cref="IGrpcServerBuilder"/> for chaining.</returns>
    public static IGrpcServerBuilder AddJsonTranscoding(this IGrpcServerBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IServiceMethodProvider<>), typeof(JsonTranscodingServiceMethodProvider<>)));

        return builder;
    }

    /// <summary>
    /// Adds gRPC JSON transcoding services to the specified <see cref="IGrpcServerBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="IGrpcServerBuilder"/>.</param>
    /// <param name="configureOptions">An <see cref="Action{GrpcJsonTranscodingOptions}"/> to configure the provided <see cref="GrpcJsonTranscodingOptions"/>.</param>
    /// <returns>The same instance of the <see cref="IGrpcServerBuilder"/> for chaining.</returns>
    public static IGrpcServerBuilder AddJsonTranscoding(this IGrpcServerBuilder builder, Action<GrpcJsonTranscodingOptions> configureOptions)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.Configure(configureOptions);
        return builder.AddJsonTranscoding();
    }
}
