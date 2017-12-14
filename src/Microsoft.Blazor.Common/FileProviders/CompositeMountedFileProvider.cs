// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace Microsoft.Blazor.Internal.Common.FileProviders
{
    // This is like a CompositeFileProvider, except that:
    // (a) the child providers can be mounted at non-root locations
    // (b) the set of contents is immutable and fully indexed at construction time
    //     so that all subsequent reads are "O(dictionary lookup)" time
    public class CompositeMountedFileProvider : InMemoryFileProvider
    {
        public CompositeMountedFileProvider(params (string, IFileProvider)[] providers)
            : base(GetCompositeContents(providers))
        {
        }

        private static IEnumerable<IFileInfo> GetCompositeContents(
            IEnumerable<(string, IFileProvider)> providers)
        {
            return providers
                .Select(pair => new { MountPoint = pair.Item1, Files = ReadAllFiles(pair.Item2, string.Empty) })
                .SelectMany(info => info.Files.Select(file => (IFileInfo)new MountedFileInfo(info.MountPoint, file)));
        }

        private static IEnumerable<IFileInfo> ReadAllFiles(IFileProvider provider, string subpath)
        {
            return provider.GetDirectoryContents(subpath).SelectMany(
                item => item.IsDirectory
                    ? ReadAllFiles(provider, item.PhysicalPath)
                    : new[] { item });
        }

        private class MountedFileInfo : IFileInfo
        {
            private readonly IFileInfo _file;

            public MountedFileInfo(string mountPoint, IFileInfo file)
            {
                _file = file;

                if (!file.PhysicalPath.StartsWith('/'))
                {
                    throw new ArgumentException($"For mounted files, {nameof(file.PhysicalPath)} must start with '/'. Value supplied: '{file.PhysicalPath}'.");
                }

                if (!mountPoint.StartsWith('/'))
                {
                    throw new ArgumentException("The path must start with '/'", nameof(mountPoint));
                }

                if (mountPoint == "/")
                {
                    PhysicalPath = file.PhysicalPath;
                }
                else
                {
                    if (mountPoint.EndsWith('/'))
                    {
                        throw new ArgumentException("Non-root paths must not end with '/'", nameof(mountPoint));
                    }

                    PhysicalPath = mountPoint + file.PhysicalPath;
                }
            }

            public bool Exists => _file.Exists;

            public long Length => _file.Length;

            public string PhysicalPath { get; }

            public string Name => _file.Name;

            public DateTimeOffset LastModified => _file.LastModified;

            public bool IsDirectory => _file.IsDirectory;

            public Stream CreateReadStream() => _file.CreateReadStream();
        }
    }
}
