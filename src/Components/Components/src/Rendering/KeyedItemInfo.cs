// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Rendering;

// Used internally during diffing to track what we know about keyed items and their positions
internal readonly struct KeyedItemInfo
{
    public readonly int OldIndex;
    public readonly int NewIndex;
    public readonly int OldSiblingIndex;
    public readonly int NewSiblingIndex;

    public KeyedItemInfo(int oldIndex, int newIndex)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
        OldSiblingIndex = -1;
        NewSiblingIndex = -1;
    }

    private KeyedItemInfo(in KeyedItemInfo copyFrom, int oldSiblingIndex, int newSiblingIndex)
    {
        this = copyFrom;
        OldSiblingIndex = oldSiblingIndex;
        NewSiblingIndex = newSiblingIndex;
    }

    public KeyedItemInfo WithOldSiblingIndex(int oldSiblingIndex)
        => new KeyedItemInfo(this, oldSiblingIndex, NewSiblingIndex);

    public KeyedItemInfo WithNewSiblingIndex(int newSiblingIndex)
        => new KeyedItemInfo(this, OldSiblingIndex, newSiblingIndex);
}
