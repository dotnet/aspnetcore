// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.Swagger.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the gRPC JSON transcoding services.
/// </summary>
public static class GrpcSwaggerServiceExtensions
{
    /// <summary>
    /// Adds gRPC JSON transcoding services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddGrpcSwagger(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGrpc().AddJsonTranscoding();

        services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, GrpcJsonTranscodingDescriptionProvider>());
        services.TryAddSingleton<DescriptorRegistry>();

        // Register default description provider in case MVC is not registered
        services.TryAddSingleton<IApiDescriptionGroupCollectionProvider>(serviceProvider =>
        {
            var actionDescriptorCollectionProvider = serviceProvider.GetService<IActionDescriptorCollectionProvider>();
            var apiDescriptionProvider = serviceProvider.GetServices<IApiDescriptionProvider>();

            return new ApiDescriptionGroupCollectionProvider(
                actionDescriptorCollectionProvider ?? new EmptyActionDescriptorCollectionProvider(),
                apiDescriptionProvider);
        });

        // Add or replace contract resolver.
        services.Replace(ServiceDescriptor.Transient<ISerializerDataContractResolver>(s =>
        {
            var serializerOptions = s.GetService<IOptions<JsonOptions>>()?.Value?.JsonSerializerOptions ?? new JsonSerializerOptions();
            var innerContractResolver = new JsonSerializerDataContractResolver(serializerOptions);
            return new GrpcDataContractResolver(innerContractResolver, s.GetRequiredService<DescriptorRegistry>());
        }));

        return services;
    }

    // Dummy type that is only used if MVC is not registered in the app
    private sealed class EmptyActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
    {
        public ActionDescriptorCollection ActionDescriptors { get; } = new ActionDescriptorCollection(new List<ActionDescriptor>(), 1);
    }
}
