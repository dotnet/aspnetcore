// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Rendering;

internal readonly struct StreamingComponentUpdate
{
    public IReadOnlyList<int> UpdatedComponentIds { get; private init; }

    public static StreamingComponentUpdate SnapshotFromRenderBatch(RenderBatch batch)
    {
        var updatedComponentIds = new List<int>();

        var updatedComponentsArray = batch.UpdatedComponents.Array;
        var updatedComponentsCount = batch.UpdatedComponents.Count;
        for (var i = 0; i < updatedComponentsCount; i++)
        {
            updatedComponentIds.Add(updatedComponentsArray[i].ComponentId);
        }

        return new StreamingComponentUpdate { UpdatedComponentIds = updatedComponentIds };
    }
}
