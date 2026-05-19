// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Represents a feature containing the path details of the original request. This feature is provided by the
/// StatusCodePagesMiddleware when it re-execute the request pipeline with an alternative path to generate the
/// response body.
/// </summary>
public interface IStatusCodeReExecuteFeature
{
    /// <summary>
    /// The <see cref="HttpRequest.PathBase"/> of the original request.
    /// </summary>
    string OriginalPathBase { get; set; }

    /// <summary>
    /// The <see cref="HttpRequest.Path"/> of the original request.
    /// </summary>
    string OriginalPath { get; set; }

    /// <summary>
    /// The <see cref="HttpRequest.QueryString"/> of the original request.
    /// </summary>
    string? OriginalQueryString { get; set; }

    /// <summary>
    /// The <see cref="HttpResponse.StatusCode"/> of the original response.
    /// </summary>
    int OriginalStatusCode => throw new NotImplementedException();

    /// <summary>
    /// Gets the selected <see cref="Http.Endpoint"/> for the original request.
    /// </summary>
    Endpoint? Endpoint => null;

    /// <summary>
    /// Gets the <see cref="RouteValueDictionary"/> associated with the original request.
    /// </summary>
    RouteValueDictionary? RouteValues => null;
}
