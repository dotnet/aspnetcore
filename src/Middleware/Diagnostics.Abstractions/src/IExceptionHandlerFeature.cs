// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Represents a feature containing the error of the original request to be examined by an exception handler.
/// </summary>
public interface IExceptionHandlerFeature
{
    /// <summary>
    /// The error encountered during the original request
    /// </summary>
    Exception Error { get; }

    /// <summary>
    /// The portion of the request path that identifies the requested resource. The value
    /// is un-escaped.
    /// </summary>
    string Path => throw new NotSupportedException();

    /// <summary>
    /// Gets the selected <see cref="Http.Endpoint"/> for the original request.
    /// </summary>
    Endpoint? Endpoint => null;

    /// <summary>
    /// Gets the <see cref="RouteValueDictionary"/> associated with the original request.
    /// </summary>
    RouteValueDictionary? RouteValues => null;
}
