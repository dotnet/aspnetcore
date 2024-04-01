// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.OpenApi.Models;

internal static class ApiDescriptionExtensions
{
    /// <summary>
    /// Maps the HTTP method of the ApiDescription to the OpenAPI <see cref="OperationType"/> .
    /// </summary>
    /// <param name="apiDescription">The ApiDescription to resolve an operation type from.</param>
    /// <returns>The <see cref="OperationType"/> associated with the given <paramref name="apiDescription"/>.</returns>
    public static OperationType ToOperationType(this ApiDescription apiDescription) =>
        apiDescription.HttpMethod?.ToUpperInvariant() switch
        {
            "GET" => OperationType.Get,
            "POST" => OperationType.Post,
            "PUT" => OperationType.Put,
            "DELETE" => OperationType.Delete,
            "PATCH" => OperationType.Patch,
            "HEAD" => OperationType.Head,
            "OPTIONS" => OperationType.Options,
            "TRACE" => OperationType.Trace,
            _ => throw new InvalidOperationException($"Unsupported HTTP method: {apiDescription.HttpMethod}"),
        };

    /// <summary>
    /// Maps the relative path included in the ApiDescription to the path
    /// that should be included in the OpenApiDocument. This typically
    /// consists of removing any constraints from route parameter parts
    /// and retaining only the literals.
    /// </summary>
    /// <param name="apiDescription">The ApiDescription to resolve an item path from.</param>
    /// <returns>The resolved item path for the given <paramref name="apiDescription"/>.</returns>
    public static string MapRelativePathToItemPath(this ApiDescription apiDescription)
    {
        Debug.Assert(apiDescription.RelativePath != null, "Relative path cannot be null.");
        var strippedRoute = new StringBuilder();
        var routePattern = RoutePatternFactory.Parse(apiDescription.RelativePath);
        for (var i = 0; i < routePattern.PathSegments.Count; i++)
        {
            strippedRoute.Append('/');
            var segment = routePattern.PathSegments[i];
            foreach (var part in segment.Parts)
            {
                if (part is RoutePatternLiteralPart literalPart)
                {
                    strippedRoute.Append(literalPart.Content);
                }
                else if (part is RoutePatternParameterPart parameterPart)
                {
                    strippedRoute.Append('{');
                    strippedRoute.Append(parameterPart.Name);
                    strippedRoute.Append('}');
                }
            }
        }
        // "" -> "/"
        if (routePattern.PathSegments.Count == 0)
        {
            strippedRoute.Append('/');
        }
        return strippedRoute.ToString();
    }
}
