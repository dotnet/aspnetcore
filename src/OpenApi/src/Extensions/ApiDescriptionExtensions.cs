// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.OpenApi.Models;

internal static class ApiDescriptionExtensions
{
    /// <summary>
    /// Maps the HTTP method of the ApiDescription to the OpenAPI <see cref="OperationType"/> .
    /// </summary>
    /// <param name="apiDescription">The ApiDescription to resolve an operation type from.</param>
    /// <returns>The <see cref="OperationType"/> associated with the given <paramref name="apiDescription"/>.</returns>
    public static OperationType GetOperationType(this ApiDescription apiDescription) =>
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
        // "" -> "/"
        if (string.IsNullOrEmpty(apiDescription.RelativePath))
        {
            return "/";
        }
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
                else if (part is RoutePatternSeparatorPart separatorPart)
                {
                    strippedRoute.Append(separatorPart.Content);
                }
            }
        }
        return strippedRoute.ToString();
    }

    /// <summary>
    /// Determines if the given <see cref="ApiParameterDescription" /> is a request body parameter.
    /// </summary>
    /// <param name="apiParameterDescription">The <see cref="ApiParameterDescription"/> to check. </param>
    /// <returns>Returns <langword ref="true"/> if the given parameter comes from the request body, <langword ref="false"/> otherwise.</returns>
    public static bool IsRequestBodyParameter(this ApiParameterDescription apiParameterDescription) =>
        apiParameterDescription.Source == BindingSource.Body ||
        apiParameterDescription.Source == BindingSource.FormFile ||
        apiParameterDescription.Source == BindingSource.Form;

    /// <summary>
    /// Retrieves the form parameters from the ApiDescription, if they exist.
    /// </summary>
    /// <param name="apiDescription">The ApiDescription to resolve form parameters from.</param>
    /// <param name="formParameters">A list of <see cref="ApiParameterDescription"/> associated with the form parameters.</param>
    /// <returns><see langword="true"/> if form parameters were found, <see langword="false"/> otherwise.</returns>
    public static bool TryGetFormParameters(this ApiDescription apiDescription, out IEnumerable<ApiParameterDescription> formParameters)
    {
        formParameters = apiDescription.ParameterDescriptions.Where(parameter => parameter.Source == BindingSource.Form || parameter.Source == BindingSource.FormFile);
        return formParameters.Any();
    }

    /// <summary>
    /// Retrieves the body parameter from the ApiDescription, if it exists.
    /// </summary>
    /// <param name="apiDescription">The ApiDescription to resolve the body parameter from.</param>
    /// <param name="bodyParameter">The <see cref="ApiParameterDescription"/> associated with the body parameter.</param>
    /// <returns><see langword="true"/> if a single body parameter was found, <see langword="false"/> otherwise.</returns>
    public static bool TryGetBodyParameter(this ApiDescription apiDescription, [NotNullWhen(true)] out ApiParameterDescription? bodyParameter)
    {
        bodyParameter = null;
        var bodyParameters = apiDescription.ParameterDescriptions.Where(parameter => parameter.Source == BindingSource.Body);
        if (bodyParameters.Count() == 1)
        {
            bodyParameter = bodyParameters.Single();
            return true;
        }
        return false;
    }
}
