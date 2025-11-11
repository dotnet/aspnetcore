// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Metadata that identifies infrastructure endpoints for Blazor framework functionality.
/// This marker is used to distinguish framework endpoints (like opaque redirection, 
/// disconnect, and JavaScript initializers) from regular component endpoints.
/// </summary>
public sealed class ComponentFrameworkEndpointMetadata
{
}