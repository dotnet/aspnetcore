// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Shared;

internal sealed class DictionaryDebugView<TKey, TValue> where TKey : notnull
{
    private readonly IDictionary<TKey, TValue> _dict;

    public DictionaryDebugView(IDictionary<TKey, TValue> dictionary)
    {
        _dict = dictionary;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public DictionaryItemDebugView<TKey, TValue>[] Items
    {
        get
        {
            var keyValuePairs = new KeyValuePair<TKey, TValue>[_dict.Count];
            _dict.CopyTo(keyValuePairs, 0);
            var items = new DictionaryItemDebugView<TKey, TValue>[keyValuePairs.Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new DictionaryItemDebugView<TKey, TValue>(keyValuePairs[i]);
            }
            return items;
        }
    }
}
