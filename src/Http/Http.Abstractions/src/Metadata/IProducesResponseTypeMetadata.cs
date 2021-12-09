// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Defines a contract for outline the response type returned from an endpoint.
/// </summary>
public interface IProducesResponseTypeMetadata
{
    /// <summary>
    /// Gets the optimistic return type of the action.
    /// </summary>
    Type? Type { get; }

    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    int StatusCode { get; }

    /// <summary>
    /// Gets the content types supported by the metadata.
    /// </summary>
    IEnumerable<string> ContentTypes { get; }
}
