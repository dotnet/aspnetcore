// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Represents persistence during prerendering.
/// </summary>
public sealed class PersistOnPrerendering : IPersistenceReason
{
    /// <summary>
    /// Gets the singleton instance of <see cref="PersistOnPrerendering"/>.
    /// </summary>
    public static readonly PersistOnPrerendering Instance = new();

    private PersistOnPrerendering() { }

    /// <inheritdoc />
    public bool PersistByDefault => true;
}