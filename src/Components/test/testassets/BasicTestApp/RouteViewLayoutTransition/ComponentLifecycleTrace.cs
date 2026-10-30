// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace BasicTestApp.RouteViewLayoutTransition;

public static class InstantiationTrace
{
    private static readonly object SyncRoot = new();
    private static readonly List<string> EntriesInternal = [];

    public static event Action? Changed;

    public static IReadOnlyList<string> Entries
    {
        get
        {
            lock (SyncRoot)
            {
                return EntriesInternal.ToArray();
            }
        }
    }

    public static void Add(string message)
    {
        lock (SyncRoot)
        {
            EntriesInternal.Insert(0, message);
        }

        Changed?.Invoke();
    }
}
