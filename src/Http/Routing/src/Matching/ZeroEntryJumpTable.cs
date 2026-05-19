// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class ZeroEntryJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;

    public ZeroEntryJumpTable(int defaultDestination, int exitDestination)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        return segment.Length == 0 ? _exitDestination : _defaultDestination;
    }

    public override string DebuggerToString()
    {
        return $"{{ $+: {_defaultDestination}, $0: {_exitDestination} }}";
    }
}
