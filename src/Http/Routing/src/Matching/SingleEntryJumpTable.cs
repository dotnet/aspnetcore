// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class SingleEntryJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly string _text;
    private readonly int _destination;

    public SingleEntryJumpTable(
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
        if (segment.Length == 0)
        {
            return _exitDestination;
        }

        if (segment.Length == _text.Length &&
            string.Compare(
                path,
                segment.Start,
                _text,
                0,
                segment.Length,
                StringComparison.OrdinalIgnoreCase) == 0)
        {
            return _destination;
        }

        return _defaultDestination;
    }

    public override string DebuggerToString()
    {
        return $"{{ {_text}: {_destination}, $+: {_defaultDestination}, $0: {_exitDestination} }}";
    }
}
