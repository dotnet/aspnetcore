// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Components.TestServer.RazorComponents.Pages.CacheBoundaryTest;

public static class HoleDriftState
{
    private static List<string> _items = new() { "a", "b", "c" };

    public static IReadOnlyList<string> Items => _items;

    public static void Reset() => _items = new() { "a", "b", "c" };

    public static void DropLast()
    {
        if (_items.Count > 0)
        {
            _items.RemoveAt(_items.Count - 1);
        }
    }
}
