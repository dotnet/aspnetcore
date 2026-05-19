// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Metadata that defines data tokens for an <see cref="Endpoint"/>. This metadata
/// type provides data tokens value for <see cref="RouteData.DataTokens"/> associated
/// with an endpoint.
/// </summary>
public interface IDataTokensMetadata
{
    /// <summary>
    /// Get the data tokens.
    /// </summary>
    IReadOnlyDictionary<string, object?> DataTokens { get; }
}
