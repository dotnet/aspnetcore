// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Writers;

/// <summary>
/// OpenAPI-related methods for <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class IEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Register an endpoint onto the current application for resolving the OpenAPI document associated
    /// with the current application.
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static RouteHandlerBuilder MapOpenApiDocument(this IEndpointRouteBuilder endpoints, string pattern = "/openapi.json") =>
        endpoints.MapGet(pattern, async ([FromServices] OpenApiDocumentService openApiDocumentService, HttpContext context) =>
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json;charset=utf-8";
                using var innerWriter = new StringWriter(CultureInfo.InvariantCulture);
                var jsonWriter = new OpenApiJsonWriter(innerWriter);
                openApiDocumentService.Document.SerializeAsV3(jsonWriter);
                await context.Response.WriteAsync(innerWriter.ToString(), new UTF8Encoding(false));
            }).ExcludeFromDescription();
}
