// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides access to the antiforgery token associated with the current session.
/// </summary>
public abstract class AntiforgeryStateProvider
{
    /// <summary>
    /// Gets the current <see cref="AntiforgeryRequestToken"/> if available.
    /// </summary>
    /// <returns>The current <see cref="AntiforgeryRequestToken"/> if available.</returns>
    public abstract AntiforgeryRequestToken? GetAntiforgeryToken();
}
