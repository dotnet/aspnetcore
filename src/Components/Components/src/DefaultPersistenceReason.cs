// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Default persistence reason used when no specific reason is provided.
/// </summary>
internal sealed class DefaultPersistenceReason : IPersistenceReason
{
    public static readonly DefaultPersistenceReason Instance = new();

    private DefaultPersistenceReason() { }

    /// <inheritdoc />
    public bool PersistByDefault => true;
}