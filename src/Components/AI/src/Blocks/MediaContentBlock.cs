// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

public class MediaContentBlock : ContentBlock
{
    private readonly List<DataContent> _items = new();

    public IReadOnlyList<DataContent> Items => _items;

    public void AddContent(DataContent content)
    {
        _items.Add(content);
    }
}
