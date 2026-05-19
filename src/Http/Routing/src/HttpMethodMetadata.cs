// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Shared;
using static Microsoft.AspNetCore.Http.HttpMethods;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Represents HTTP method metadata used during routing.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class HttpMethodMetadata : IHttpMethodMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMethodMetadata" /> class.
    /// </summary>
    /// <param name="httpMethods">
    /// The HTTP methods used during routing.
    /// An empty collection means any HTTP method will be accepted.
    /// </param>
    public HttpMethodMetadata(IEnumerable<string> httpMethods)
        : this(httpMethods, acceptCorsPreflight: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMethodMetadata" /> class.
    /// </summary>
    /// <param name="httpMethods">
    /// The HTTP methods used during routing.
    /// An empty collection means any HTTP method will be accepted.
    /// </param>
    /// <param name="acceptCorsPreflight">A value indicating whether routing accepts CORS preflight requests.</param>
    public HttpMethodMetadata(IEnumerable<string> httpMethods, bool acceptCorsPreflight)
    {
        ArgumentNullException.ThrowIfNull(httpMethods);

        HttpMethods = httpMethods.Select(GetCanonicalizedValue).ToArray();
        AcceptCorsPreflight = acceptCorsPreflight;
    }

    /// <summary>
    /// Returns a value indicating whether the associated endpoint should accept CORS preflight requests.
    /// </summary>
    public bool AcceptCorsPreflight { get; set; }

    /// <summary>
    /// Returns a read-only collection of HTTP methods used during routing.
    /// An empty collection means any HTTP method will be accepted.
    /// </summary>
    public IReadOnlyList<string> HttpMethods { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(HttpMethods), HttpMethods, "Cors", AcceptCorsPreflight);
    }
}
