// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Shared;

// This type is designed to be a debug proxy for dictionary types.
// The constructor accepts an enumerable because many of the header collection types implement IHeaderDictionary which doesn't directly implement IDictionary.
internal sealed class StringValuesDictionaryDebugView
{
    private readonly IEnumerable<KeyValuePair<string, StringValues>> _enumerable;

    public StringValuesDictionaryDebugView(IEnumerable<KeyValuePair<string, StringValues>> enumerable)
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
