// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Writers;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// OpenAPI-related methods for <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class OpenApiEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Register an endpoint onto the current application for resolving the OpenAPI document associated
    /// with the current application.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="pattern">The route to register the endpoint on. Must include the 'documentName' route parameter.</param>
    /// <returns>An <see cref="IEndpointRouteBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapOpenApi(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern = OpenApiConstants.DefaultOpenApiRoute)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenApiOptions>>();
        return endpoints.MapGet(pattern, async (HttpContext context, string documentName = OpenApiConstants.DefaultDocumentName) =>
            {
                // We need to retrieve the document name in a case-insensitive manner to support case-insensitive document name resolution.
                // The document service is registered with a key equal to the document name, but in lowercase.
                // The GetRequiredKeyedService() method is case-sensitive, which doesn't work well for OpenAPI document names here,
                // as the document name is also used as the route to retrieve the document, so we need to ensure this is lowercased to achieve consistency with ASP.NET Core routing.
                // The same goes for the document options below, which is also case-sensitive, and thus we need to pass in a case-insensitive document name.
                // See OpenApiServiceCollectionExtensions.cs for more info.
                var lowercasedDocumentName = documentName.ToLowerInvariant();

                // It would be ideal to use the `HttpResponseStreamWriter` to
                // asynchronously write to the response stream here but Microsoft.OpenApi
                // does not yet support async APIs on their writers.
                // See https://github.com/microsoft/OpenAPI.NET/issues/421 for more info.
                var documentService = context.RequestServices.GetKeyedService<OpenApiDocumentService>(lowercasedDocumentName);
                if (documentService is null)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "text/plain;charset=utf-8";
                    await context.Response.WriteAsync($"No OpenAPI document with the name '{lowercasedDocumentName}' was found.");
                }
                else
                {
                    var document = await documentService.GetOpenApiDocumentAsync(context.RequestServices, context.RequestAborted);
                    var documentOptions = options.Get(lowercasedDocumentName);
                    using var output = MemoryBufferWriter.Get();
                    using var writer = Utf8BufferTextWriter.Get(output);
                    try
                    {
                        if (UseYaml(pattern))
                        {
                            await document.SerializeAsync(new OpenApiYamlWriter(writer), documentOptions.OpenApiVersion);
                            context.Response.ContentType = "text/plain+yaml;charset=utf-8";
                        }
                        else
                        {
                            await document.SerializeAsync(new OpenApiJsonWriter(writer), documentOptions.OpenApiVersion);
                            context.Response.ContentType = "application/json;charset=utf-8";
                        }
                        await context.Response.BodyWriter.WriteAsync(output.ToArray(), context.RequestAborted);
                        await context.Response.BodyWriter.FlushAsync(context.RequestAborted);
                    }
                    finally
                    {
                        MemoryBufferWriter.Return(output);
                        Utf8BufferTextWriter.Return(writer);
                    }

                }
            }).ExcludeFromDescription();
    }

    private static bool UseYaml(string pattern) =>
        pattern.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
        pattern.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
}
