// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Represents HTTP method metadata used during routing.
/// </summary>
public interface IHttpMethodMetadata
{
    /// <summary>
    /// Returns a value indicating whether the associated endpoint should accept CORS preflight requests.
    /// </summary>
    bool AcceptCorsPreflight
    {
        get => false;
        set => throw new NotImplementedException();
    }

    /// <summary>
    /// Returns a read-only collection of HTTP methods used during routing.
    /// An empty collection means any HTTP method will be accepted.
    /// </summary>
    IReadOnlyList<string> HttpMethods { get; }
}
