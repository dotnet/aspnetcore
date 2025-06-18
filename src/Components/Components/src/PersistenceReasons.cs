// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents persistence during prerendering.
/// </summary>
public class PersistOnPrerendering : IPersistenceReason
{
    /// <inheritdoc />
    public bool PersistByDefault { get; } = true;
}

/// <summary>
/// Represents persistence during enhanced navigation.
/// </summary>
public class PersistOnEnhancedNavigation : IPersistenceReason
{
    /// <inheritdoc />
    public bool PersistByDefault { get; }
}

/// <summary>
/// Represents persistence when a circuit is paused.
/// </summary>
public class PersistOnCircuitPause : IPersistenceReason
{
    /// <inheritdoc />
    public bool PersistByDefault { get; } = true;
}