// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class LinearSearchJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly (string text, int destination)[] _entries;

    public LinearSearchJumpTable(
        int defaultDestination,
        int exitDestination,
        (string text, int destination)[] entries)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;
        _entries = entries;
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        if (segment.Length == 0)
        {
            return _exitDestination;
        }

        var entries = _entries;
        var pathSpan = path.AsSpan(segment.Start, segment.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            var text = entries[i].text;
            if (pathSpan.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                return entries[i].destination;
            }
        }

        return _defaultDestination;
    }

    public override string DebuggerToString()
    {
        var builder = new StringBuilder();
        builder.Append("{ ");

        builder.AppendJoin(", ", _entries.Select(e => $"{e.text}: {e.destination}"));

        builder.Append("$+: ");
        builder.Append(_defaultDestination);
        builder.Append(", ");

        builder.Append("$0: ");
        builder.Append(_defaultDestination);

        builder.Append(" }");

        return builder.ToString();
    }
}
