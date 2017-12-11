// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using System.IO;
using System;

namespace Microsoft.Blazor.Internal.Common.FileProviders
{
    internal class InMemoryFileInfo : IFileInfo
    {
        private readonly bool _exists;
        private readonly bool _isDirectory;
        private readonly byte[] _fileData;
        private readonly string _name;
        private readonly string _physicalPath;
        private readonly DateTimeOffset _lastModified;

        public static IFileInfo ForExistingDirectory(string path)
            => new InMemoryFileInfo(path, isDir: true, exists: true);

        public static IFileInfo ForExistingFile(string path, Stream dataStream, DateTimeOffset lastModified)
            => new InMemoryFileInfo(path, isDir: false, exists: true, dataStream: dataStream, lastModified: lastModified);

        public static IFileInfo ForNonExistingFile(string path)
            => new InMemoryFileInfo(path, isDir: false, exists: false);

        private InMemoryFileInfo(string physicalPath, bool isDir, bool exists, Stream dataStream = null, DateTimeOffset lastModified = default(DateTimeOffset))
        {
            _exists = exists;
            _isDirectory = isDir;
            _name = Path.GetFileName(physicalPath);
            _physicalPath = physicalPath ?? throw new ArgumentNullException(nameof(physicalPath));
            _lastModified = lastModified;

            if (_exists)
            {
                if (!_physicalPath.StartsWith(InMemoryFileProvider.DirectorySeparatorChar))
                {
                    throw new ArgumentException($"Must start with '{InMemoryFileProvider.DirectorySeparatorChar}'",
                        nameof(physicalPath));
                }

                if (_physicalPath.EndsWith(InMemoryFileProvider.DirectorySeparatorChar))
                {
                    throw new ArgumentException($"Must not end with '{InMemoryFileProvider.DirectorySeparatorChar}'",
                        nameof(physicalPath));
                }
            }

            if (dataStream != null)
            {
                using (var ms = new MemoryStream())
                {
                    dataStream.CopyTo(ms);
                    _fileData = ms.ToArray();
                    dataStream.Dispose();
                }
            }
        }

        public bool Exists => _exists;

        public long Length => Exists && !IsDirectory
            ? _fileData.Length
            : throw new InvalidOperationException(IsDirectory
                ? "The item is a directory."
                : "The item does not exist.");

        public string PhysicalPath => _physicalPath;

        public string Name => _name;

        public DateTimeOffset LastModified => _lastModified;

        public bool IsDirectory => _isDirectory;

        public Stream CreateReadStream() => Exists && !IsDirectory
            ? new MemoryStream(_fileData)
            : throw new InvalidOperationException(IsDirectory
                ? "The item is a directory."
                : "The item does not exist.");
    }
}
