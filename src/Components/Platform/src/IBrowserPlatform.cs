// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Provides access to browser platform APIs.
/// </summary>
public interface IBrowserPlatform
{
    /// <summary>
    /// Gets the active browser window.
    /// </summary>
    Window Window { get; }
}
