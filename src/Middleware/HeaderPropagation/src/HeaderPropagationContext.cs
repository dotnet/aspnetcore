// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// A context object for <see cref="HeaderPropagationEntry.ValueFilter"/> delegates.
/// </summary>
public readonly struct HeaderPropagationContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="HeaderPropagationContext"/> with the provided
    /// <paramref name="httpContext"/>, <paramref name="headerName"/> and <paramref name="headerValue"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="Http.HttpContext"/> associated with the current request.</param>
    /// <param name="headerName">The header name.</param>
    /// <param name="headerValue">The header value present in the current request.</param>
    public HeaderPropagationContext(HttpContext httpContext, string headerName, StringValues headerValue)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(headerName);

        HttpContext = httpContext;
        HeaderName = headerName;
        HeaderValue = headerValue;
    }

    /// <summary>
    /// Gets the <see cref="Http.HttpContext"/> associated with the current request.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Gets the header name.
    /// </summary>
    public string HeaderName { get; }

    /// <summary>
    /// Gets the header value from the current request.
    /// </summary>
    public StringValues HeaderValue { get; }
}
