// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Rendering;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal readonly struct RenderQueueEntry
{
    public readonly ComponentState ComponentState;
    public readonly RenderFragment RenderFragment;

    public RenderQueueEntry(ComponentState componentState, RenderFragment renderFragment)
    {
        ComponentState = componentState;
        RenderFragment = renderFragment ?? throw new ArgumentNullException(nameof(renderFragment));
    }

    private string GetDebuggerDisplay()
    {
        return $"ComponentId = {ComponentState.ComponentId}, Type = {ComponentState.Component.GetType().Name}";
    }
}
