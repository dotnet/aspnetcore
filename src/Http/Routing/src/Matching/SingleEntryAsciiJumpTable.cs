// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Routing.Matching;

// Optimized implementation for cases where we know that we're
// comparing to ASCII.
internal sealed class SingleEntryAsciiJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly string _text;
    private readonly int _destination;

    public SingleEntryAsciiJumpTable(
        int defaultDestination,
        int exitDestination,
        string text,
        int destination)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;
        _text = text;
        _destination = destination;
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        var length = segment.Length;
        if (length == 0)
        {
            return _exitDestination;
        }

        var text = _text;
        if (length != text.Length)
        {
            return _defaultDestination;
        }

        var a = path.AsSpan(segment.Start, length);
        var b = text.AsSpan();

        Debug.Assert(a.Length == b.Length && b.Length == length);

        return Ascii.EqualsIgnoreCase(a, b) ? _destination : _defaultDestination;
    }

    public override string DebuggerToString()
    {
        return $"{{ {_text}: {_destination}, $+: {_defaultDestination}, $0: {_exitDestination} }}";
    }
}
