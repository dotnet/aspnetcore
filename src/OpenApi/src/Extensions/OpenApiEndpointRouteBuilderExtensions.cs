// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
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
    public static IEndpointConventionBuilder MapOpenApi(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern = "/openapi/{documentName}.json")
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenApiOptions>>();
        return endpoints.MapGet(pattern, async (string documentName, HttpContext context) =>
            {
                // It would be ideal to use the `HttpResponseStreamWriter` to
                // asynchronously write to the response stream here but Microsoft.OpenApi
                // does not yet support async APIs on their writers.
                // See https://github.com/microsoft/OpenAPI.NET/issues/421 for more info.
                var documentService = context.RequestServices.GetRequiredKeyedService<OpenApiDocumentService>(documentName);
                var document = await documentService.GetOpenApiDocumentAsync();
                var documentOptions = options.Get(documentName);
                using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
                var jsonWriter = new OpenApiJsonWriter(stringWriter);
                document.Serialize(jsonWriter, documentOptions.OpenApiVersion);
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json;charset=utf-8";
                await context.Response.WriteAsync(stringWriter.ToString(), new UTF8Encoding(false));
            }).ExcludeFromDescription();
    }
}
