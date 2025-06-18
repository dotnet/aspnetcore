// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal readonly struct PersistComponentStateRegistration(
    Func<Task> callback,
    IComponentRenderMode? renderMode,
    IReadOnlyList<IPersistenceReasonFilter>? reasonFilters = null)
{
    public Func<Task> Callback { get; } = callback;

    public IComponentRenderMode? RenderMode { get; } = renderMode;

    public IReadOnlyList<IPersistenceReasonFilter>? ReasonFilters { get; } = reasonFilters;
}
