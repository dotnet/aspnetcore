// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Indicates whether or not that API explorer data should be emitted for this endpoint.
/// </summary>
public interface IExcludeFromDescriptionMetadata
{
    /// <summary>
    /// Gets a value indicating whether OpenAPI
    /// data should be excluded for this endpoint. If <see langword="true"/>,
    /// API metadata is not emitted.
    /// </summary>
    bool ExcludeFromDescription { get; }
}
