// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

internal static class InputRenderer
{
    private static TestRenderer? _testRenderer;

    public static async Task<TComponent> RenderAndGetComponent<TValue, TComponent>(TestInputHostComponent<TValue, TComponent> hostComponent)
    where TComponent : InputBase<TValue>
    {
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(hostComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        return FindComponent<TComponent>(testRenderer.Batches.Single());
    }

    public static async Task<int> RenderAndGetId<TValue, TComponent>(TestInputHostComponent<TValue, TComponent> hostComponent)
        where TComponent : InputBase<TValue>
    {
        _testRenderer = new TestRenderer();
        var componentId = _testRenderer.AssignRootComponentId(hostComponent);
        await _testRenderer.RenderRootComponentAsync(componentId);
        return componentId;
    }

    public static ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId)
    {
        return _testRenderer!.GetCurrentRenderTreeFrames(componentId);
    }

    private static TComponent FindComponent<TComponent>(CapturedBatch batch)
        => batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Select(f => f.Component)
                .OfType<TComponent>()
                .Single();
}
