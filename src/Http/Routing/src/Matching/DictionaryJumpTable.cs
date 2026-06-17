// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class DictionaryJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly FrozenDictionary<string, int> _dictionary;
    private readonly FrozenDictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> _lookup;

    public DictionaryJumpTable(
        int defaultDestination,
        int exitDestination,
        (string text, int destination)[] entries)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;

        _dictionary = entries.ToFrozenDictionary(e => e.text, e => e.destination, StringComparer.OrdinalIgnoreCase);
        _lookup = _dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        if (segment.Length == 0)
        {
            return _exitDestination;
        }

        var text = path.AsSpan(segment.Start, segment.Length);
        if (_lookup.TryGetValue(text, out var destination))
        {
            return destination;
        }

        return _defaultDestination;
    }

    public override string DebuggerToString()
    {
        var builder = new StringBuilder();
        builder.Append("{ ");

        builder.AppendJoin(", ", _dictionary.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

        builder.Append("$+: ");
        builder.Append(_defaultDestination);
        builder.Append(", ");

        builder.Append("$0: ");
        builder.Append(_defaultDestination);

        builder.Append(" }");

        return builder.ToString();
    }
}
