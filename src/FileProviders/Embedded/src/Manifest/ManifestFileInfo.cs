// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    internal class ManifestFileInfo : IFileInfo
    {
        private long? _length;

        public ManifestFileInfo(Assembly assembly, ManifestFile file, DateTimeOffset lastModified)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            Assembly = assembly;
            ManifestFile = file;
            LastModified = lastModified;
        }

        public Assembly Assembly { get; }

        public ManifestFile ManifestFile { get; }

        public bool Exists => true;

        public long Length => EnsureLength();

        public string PhysicalPath => null;

        public string Name => ManifestFile.Name;

        public DateTimeOffset LastModified { get; }

        public bool IsDirectory => false;

        private long EnsureLength()
        {
            if (_length == null)
            {
                using (var stream = Assembly.GetManifestResourceStream(ManifestFile.ResourcePath))
                {
                    _length = stream.Length;
                }
            }

            return _length.Value;
        }

        public Stream CreateReadStream()
        {
            var stream = Assembly.GetManifestResourceStream(ManifestFile.ResourcePath);
            if (!_length.HasValue)
            {
                _length = stream.Length;
            }

            return stream;
        }
    }
}
