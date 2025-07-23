// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Metadata that indicates the endpoint is an API intended for programmatic access rather than direct browser navigation.
/// When present, authentication handlers should prefer returning status codes over browser redirects.
/// </summary>
public interface IApiEndpointMetadata
{
}
