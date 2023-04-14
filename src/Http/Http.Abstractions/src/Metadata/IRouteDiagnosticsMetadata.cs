// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Interface for specifing diagnostics text for a route.
/// </summary>
public interface IRouteDiagnosticsMetadata
{
    /// <summary>
    /// Gets diagnostics text for a route.
    /// </summary>
    string Route { get; }
}
