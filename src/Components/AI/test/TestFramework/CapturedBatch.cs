// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI.Tests.TestFramework;

internal sealed class CapturedBatch
{
    private readonly List<int> _updatedComponentIds = new();
    private readonly List<int> _disposedComponentIds = new();

    internal CapturedBatch(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public IReadOnlyList<int> UpdatedComponentIds => _updatedComponentIds;

    public IReadOnlyList<int> DisposedComponentIds => _disposedComponentIds;

    internal void AddUpdatedComponent(int componentId)
    {
        _updatedComponentIds.Add(componentId);
    }

    internal void AddDisposedComponent(int componentId)
    {
        _disposedComponentIds.Add(componentId);
    }
}
