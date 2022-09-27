// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

internal static class InputRenderer
{
    public static async Task<TComponent> RenderAndGetComponent<TValue, TComponent>(TestInputHostComponent<TValue, TComponent> hostComponent)
    where TComponent : InputBase<TValue>
    {
        var testRenderer = new TestRenderer();
        var componentId = testRenderer.AssignRootComponentId(hostComponent);
        await testRenderer.RenderRootComponentAsync(componentId);
        return FindComponent<TComponent>(testRenderer.Batches.Single());
    }

    private static TComponent FindComponent<TComponent>(CapturedBatch batch)
        => batch.ReferenceFrames
                .Where(f => f.FrameType == RenderTreeFrameType.Component)
                .Select(f => f.Component)
                .OfType<TComponent>()
                .Single();
}
