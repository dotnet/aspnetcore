// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Shared;

internal sealed class EnumerableStringValuesDebugView
{
    private readonly IEnumerable<KeyValuePair<string, StringValues>> _enumerable;

    public EnumerableStringValuesDebugView(IEnumerable<KeyValuePair<string, StringValues>> enumerable)
    {
        _enumerable = enumerable;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public DictionaryItemDebugView<string, string>[] Items
    {
        get
        {
            var keyValuePairs = new List<DictionaryItemDebugView<string, string>>();
            foreach (var kvp in _enumerable)
            {
                keyValuePairs.Add(new DictionaryItemDebugView<string, string>(kvp.Key, kvp.Value.ToString()));
            }
            return keyValuePairs.ToArray();
        }
    }
}
