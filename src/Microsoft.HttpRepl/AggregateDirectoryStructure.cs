// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.HttpRepl
{
    public class AggregateDirectoryStructure : IDirectoryStructure
    {
        private readonly IDirectoryStructure _first;
        private readonly IDirectoryStructure _second;

        public AggregateDirectoryStructure(IDirectoryStructure first, IDirectoryStructure second)
        {
            _first = first;
            _second = second;
        }

        public IEnumerable<string> DirectoryNames
        {
            get
            {
                HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                values.UnionWith(_first.DirectoryNames);
                values.UnionWith(_second.DirectoryNames);
                return values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
            }
        }

        public IDirectoryStructure Parent => _first.Parent ?? _second.Parent;

        public IDirectoryStructure GetChildDirectory(string name)
        {
            return new AggregateDirectoryStructure(_first.GetChildDirectory(name), _second.GetChildDirectory(name));
        }

        public IRequestInfo RequestInfo => _first.RequestInfo ?? _second.RequestInfo;
    }
}
