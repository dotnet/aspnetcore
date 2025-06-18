// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a reason for persisting component state.
/// </summary>
public interface IPersistenceReason
{
    /// <summary>
    /// Gets a value indicating whether state should be persisted by default for this reason.
    /// </summary>
    bool PersistByDefault { get; }
}