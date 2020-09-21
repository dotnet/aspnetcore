// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Embedded.Manifest
{
    internal class ManifestDirectoryInfo : IFileInfo
    {
        public ManifestDirectoryInfo(ManifestDirectory directory, DateTimeOffset lastModified)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            Directory = directory;
            LastModified = lastModified;
        }

        public bool Exists => true;

        public long Length => -1;

        public string PhysicalPath => null;

        public string Name => Directory.Name;

        public DateTimeOffset LastModified { get; }

        public bool IsDirectory => true;

        public ManifestDirectory Directory { get; }

        public Stream CreateReadStream() =>
            throw new InvalidOperationException("Cannot create a stream for a directory.");
    }
}
