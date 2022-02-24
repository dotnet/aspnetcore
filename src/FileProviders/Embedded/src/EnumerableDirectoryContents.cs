// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.FileProviders.Embedded;

internal class EnumerableDirectoryContents : IDirectoryContents
{
    private readonly IEnumerable<IFileInfo> _entries;

    public EnumerableDirectoryContents(IEnumerable<IFileInfo> entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        _entries = entries;
    }

    public bool Exists
    {
        get { return true; }
    }

    public IEnumerator<IFileInfo> GetEnumerator()
    {
        return _entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _entries.GetEnumerator();
    }
}
