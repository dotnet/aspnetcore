// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite;

internal sealed class BackReferenceCollection
{
    private readonly List<string> _backReferences = new List<string>();

    public BackReferenceCollection(GroupCollection references)
    {
        if (references != null)
        {
            for (var i = 0; i < references.Count; i++)
            {
                _backReferences.Add(references[i].Value);
            }
        }
    }

    public BackReferenceCollection(string reference)
    {
        _backReferences.Add(reference);
    }

    public string this[int index]
    {
        get
        {
            if (index < _backReferences.Count)
            {
                return _backReferences[index];
            }
            else
            {
                throw new ArgumentOutOfRangeException(null, $"Cannot access back reference at index {index}. Only {_backReferences.Count} back references were captured.");
            }
        }
    }

    public void Add(BackReferenceCollection references)
    {
        if (references != null)
        {
            _backReferences.AddRange(references._backReferences);
        }
    }
}
