// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class BackReferenceCollection
    {
        private List<string> _backReferences = new List<string>();

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
                    throw new IndexOutOfRangeException($"Cannot access back reference at index {index}. Only {_backReferences.Count} back references were captured.");
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
}
