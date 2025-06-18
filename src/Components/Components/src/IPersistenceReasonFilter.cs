// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Filters component state persistence based on the reason for persistence.
/// </summary>
public interface IPersistenceReasonFilter
{
    /// <summary>
    /// Determines whether state should be persisted for the given reason.
    /// </summary>
    /// <param name="reason">The reason for persistence.</param>
    /// <returns><c>true</c> to persist state, <c>false</c> to skip persistence, or <c>null</c> to defer to other filters or default behavior.</returns>
    bool? ShouldPersist(IPersistenceReason reason);
}