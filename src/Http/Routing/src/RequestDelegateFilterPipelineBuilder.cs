// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal static class RequestDelegateFilterPipelineBuilder
{
    // Due to https://github.com/dotnet/aspnetcore/issues/41330 we cannot reference the EmptyHttpResult type
    // but users still need to assert on it as in https://github.com/dotnet/aspnetcore/issues/45063
    // so we temporarily work around this here by using reflection to get the actual type.
    private static readonly object? EmptyHttpResultInstance = Type.GetType("Microsoft.AspNetCore.Http.HttpResults.EmptyHttpResult, Microsoft.AspNetCore.Http.Results")?.GetProperty("Instance")?.GetValue(null, null);

    public static RequestDelegate Create(RequestDelegate requestDelegate, RequestDelegateFactoryOptions options)
    {
        Debug.Assert(options.EndpointBuilder != null);

        var serviceProvider = options.ServiceProvider ?? options.EndpointBuilder.ApplicationServices;
        var jsonOptions = serviceProvider?.GetService<IOptions<JsonOptions>>()?.Value ?? new JsonOptions();
        var jsonSerializerOptions = jsonOptions.SerializerOptions;

        var factoryContext = new EndpointFilterFactoryContext
        {
            MethodInfo = requestDelegate.Method,
            ApplicationServices = options.EndpointBuilder.ApplicationServices
        };
        var jsonTypeInfo = (JsonTypeInfo<object>)jsonSerializerOptions.GetTypeInfo(typeof(object));

        EndpointFilterDelegate filteredInvocation = async (EndpointFilterInvocationContext context) =>
        {
            if (context.HttpContext.Response.StatusCode >= 400)
            {
                return EmptyHttpResultInstance;
            }
            else
            {
                await requestDelegate(context.HttpContext);
                return EmptyHttpResultInstance;
            }
        };

        var initialFilteredInvocation = filteredInvocation;
        for (var i = options.EndpointBuilder.FilterFactories.Count - 1; i >= 0; i--)
        {
            var currentFilterFactory = options.EndpointBuilder.FilterFactories[i];
            filteredInvocation = currentFilterFactory(factoryContext, filteredInvocation);
        }

        // The filter factories have run without modifying per-request behavior, we can skip running the pipeline.
        if (ReferenceEquals(initialFilteredInvocation, filteredInvocation))
        {
            return requestDelegate;
        }

        return async (HttpContext httpContext) =>
        {
            var obj = await filteredInvocation(new DefaultEndpointFilterInvocationContext(httpContext, new object[] { httpContext }));
            if (obj is not null)
            {
                await ExecuteHandlerHelper.ExecuteReturnAsync(obj, httpContext, jsonSerializerOptions, jsonTypeInfo);
            }
        };
    }
}
