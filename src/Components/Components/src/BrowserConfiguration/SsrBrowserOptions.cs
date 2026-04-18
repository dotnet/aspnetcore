// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Serializable subset of <c>SsrStartOptions</c>.
/// </summary>
public sealed class SsrBrowserOptions
{
    /// <summary>
    /// When true, disables DOM preservation during enhanced navigation.
    /// Maps to <c>SsrStartOptions.disableDomPreservation</c>.
    /// </summary>
    public bool? DisableDomPreservation { get; set; }

    /// <summary>
    /// Timeout in milliseconds before an inactive circuit is disposed.
    /// Maps to <c>SsrStartOptions.circuitInactivityTimeoutMs</c>.
    /// </summary>
    public int? CircuitInactivityTimeoutMs { get; set; }
}
