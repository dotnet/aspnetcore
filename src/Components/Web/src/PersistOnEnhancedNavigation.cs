// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Represents persistence during enhanced navigation.
/// </summary>
public sealed class PersistOnEnhancedNavigation : IPersistenceReason
{
    /// <summary>
    /// Gets the singleton instance of <see cref="PersistOnEnhancedNavigation"/>.
    /// </summary>
    public static readonly PersistOnEnhancedNavigation Instance = new();

    private PersistOnEnhancedNavigation() { }

    /// <inheritdoc />
    public bool PersistByDefault => false;
}