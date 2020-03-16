// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class PortablePdbReader : IDisposable
    {
        private readonly Dictionary<string, MetadataReaderProvider> _cache =
            new Dictionary<string, MetadataReaderProvider>(StringComparer.Ordinal);

        public void Dispose()
        {
            foreach (var entry in _cache)
            {
                entry.Value?.Dispose();
            }

            _cache.Clear();
        }
    }
}
