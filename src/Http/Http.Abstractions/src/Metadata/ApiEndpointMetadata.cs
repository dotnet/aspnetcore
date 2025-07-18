// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Metadata that indicates the endpoint is intended for API clients.
/// When present, authentication handlers should prefer returning status codes over browser redirects.
/// </summary>
internal sealed class ApiEndpointMetadata : IApiEndpointMetadata
{
    /// <summary>
    /// Singleton instance of <see cref="ApiEndpointMetadata"/>.
    /// </summary>
    public static readonly ApiEndpointMetadata Instance = new();

    private ApiEndpointMetadata()
    {
    }
}
