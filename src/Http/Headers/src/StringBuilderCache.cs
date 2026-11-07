// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Net.Http.Headers;

// Caches one StringBuilder per thread for formatting header values. Builders never leave the
// formatting methods; only the resulting string is returned to callers.
internal static class StringBuilderCache
{
    // Header values are short; don't hold on to large buffers.
    private const int MaxBuilderSize = 360;
    private const int DefaultCapacity = 64;

    [ThreadStatic]
    private static StringBuilder? t_cachedInstance;

    public static StringBuilder Acquire()
    {
        var sb = t_cachedInstance;
        if (sb is not null)
        {
            t_cachedInstance = null;
            sb.Clear();
            return sb;
        }

        return new StringBuilder(DefaultCapacity);
    }

    public static string GetStringAndRelease(StringBuilder sb)
    {
        var result = sb.ToString();
        if (sb.Capacity <= MaxBuilderSize)
        {
            t_cachedInstance = sb;
        }
        return result;
    }
}
