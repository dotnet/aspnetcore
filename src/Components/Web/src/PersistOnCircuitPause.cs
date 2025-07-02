// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Represents persistence when a circuit is paused.
/// </summary>
public sealed class PersistOnCircuitPause : IPersistenceReason
{
    /// <summary>
    /// Gets the singleton instance of <see cref="PersistOnCircuitPause"/>.
    /// </summary>
    public static readonly PersistOnCircuitPause Instance = new();

    private PersistOnCircuitPause() { }

    /// <inheritdoc />
    public bool PersistByDefault => true;
}