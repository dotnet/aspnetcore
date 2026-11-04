// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        int count = 0;
        int start = 1; // Paths always start with a leading /
        int end;
        var span = path.AsSpan(start);
        while ((end = span.IndexOf('/')) >= 0 && count < segments.Length)
        {
            segments[count++] = new PathSegment(start, end);
            start += end + 1; // resume search after the current character
            span = path.AsSpan(start);
        }

        // Residue
        var length = span.Length;
        if (length > 0 && count < segments.Length)
        {
            segments[count++] = new PathSegment(start, length);
        }

        return count;
    }
}
