// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Microsoft.AspNetCore.Routing.Matching;

// Low level implementation of our path tokenization algorithm. Alternative
// to PathTokenizer.
internal static class FastPathTokenizer
{
    // This section tokenizes the path by marking the sequence of slashes, and their
    // and the length of the text between them.
    //
    // If there is residue (text after last slash) then the length of the segment will
    // computed based on the string length.
    public static int Tokenize(string path, Span<PathSegment> segments)
    {
        // This can happen in test scenarios.
        if (string.IsNullOrEmpty(path) || segments.IsEmpty)
        {
            return 0;
        }

        var count = 0;
        var start = 1; // Paths always start with a leading /
        var i = 1;

        // Paths usually consist of several short segments. Rather than searching for each slash separately,
        // compare a vector of characters at a time and enumerate the slashes found in it.
        if (Vector128.IsHardwareAccelerated)
        {
            ref var pathRef = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(path.AsSpan()));
            var slash = Vector128.Create((ushort)'/');
            var lastVectorStart = path.Length - Vector128<ushort>.Count;
            for (; i <= lastVectorStart; i += Vector128<ushort>.Count)
            {
                var mask = Vector128.Equals(Vector128.LoadUnsafe(ref pathRef, (nuint)i), slash).ExtractMostSignificantBits();
                while (mask != 0)
                {
                    var end = i + BitOperations.TrailingZeroCount(mask);
                    segments[count++] = new PathSegment(start, end - start);
                    if (count == segments.Length)
                    {
                        return count;
                    }

                    start = end + 1; // resume after the current character
                    mask &= mask - 1;
                }
            }
        }

        for (; i < path.Length; i++)
        {
            if (path[i] == '/')
            {
                segments[count++] = new PathSegment(start, i - start);
                if (count == segments.Length)
                {
                    return count;
                }

                start = i + 1; // resume after the current character
            }
        }

        // Residue
        if (start < path.Length)
        {
            segments[count++] = new PathSegment(start, path.Length - start);
        }

        return count;
    }
}
