// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Blazor.Internal.Common.FileProviders
{
    public class EmbeddedResourceFileProvider : InMemoryFileProvider
    {
        public EmbeddedResourceFileProvider(Assembly assembly, string resourceNamePrefix)
            : base(ReadEmbeddedResources(assembly, resourceNamePrefix))
        {
        }

        private static IEnumerable<(string, byte[])> ReadEmbeddedResources(
            Assembly assembly, string resourceNamePrefix)
        {
            return assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(resourceNamePrefix, StringComparison.Ordinal))
                .Select(name => (
                    name.Substring(resourceNamePrefix.Length),
                    ReadManifestResource(assembly, name)));
        }

        private static byte[] ReadManifestResource(Assembly assembly, string name)
        {
            using (var ms = new MemoryStream())
            using (var resourceStream = assembly.GetManifestResourceStream(name))
            {
                resourceStream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
