// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Collections;
using System;

namespace Microsoft.AspNetCore.Blazor.Internal.Common.FileProviders
{
    internal class InMemoryDirectoryContents : IDirectoryContents
    {
        private readonly Dictionary<string, object> _names; // Just to ensure there are no duplicate names
        private readonly List<IFileInfo> _contents;

        public InMemoryDirectoryContents(IEnumerable<IFileInfo> contents)
        {
            if (contents != null)
            {
                _contents = contents.ToList();
                _names = _contents.ToDictionary(item => item.Name, item => (object)null);
            }
        }

        public bool Exists => _contents != null;

        public IEnumerator<IFileInfo> GetEnumerator() => GetEnumeratorIfExists();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorIfExists();

        internal void AddItem(IFileInfo item)
        {
            _names.Add(item.Name, null); // Asserts uniqueness
            _contents.Add(item);
        }
        internal bool ContainsName(string name)
            => _names.ContainsKey(name);

        private IEnumerator<IFileInfo> GetEnumeratorIfExists() => Exists
            ? _contents.GetEnumerator()
            : throw new InvalidOperationException("The directory does not exist");
    }
}
