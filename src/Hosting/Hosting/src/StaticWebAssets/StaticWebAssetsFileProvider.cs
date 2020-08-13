// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Hosting.StaticWebAssets
{
    // A file provider used for serving static web assets from referenced projects and packages during development.
    // The file provider maps folders from referenced projects and packages and prepends a prefix to their relative
    // paths.
    // At publish time the assets end up in the wwwroot folder of the published app under the prefix indicated here
    // as the base path.
    // For example, for a referenced project mylibrary with content under <<mylibrarypath>>\wwwroot will expose
    // static web assets under _content/mylibrary (this is by convention). The path prefix or base path we apply
    // is that (_content/mylibrary).
    // when the app gets published, the build pipeline puts the static web assets for mylibrary under
    // publish/wwwroot/_content/mylibrary/sample-asset.js
    // To allow for the same experience during development, StaticWebAssetsFileProvider maps the contents of
    // <<mylibrarypath>>\wwwroot\** to _content/mylibrary/**
    internal class StaticWebAssetsFileProvider : IFileProvider
    {
        private static readonly StringComparison FilePathComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            StringComparison.OrdinalIgnoreCase :
            StringComparison.Ordinal;

        public StaticWebAssetsFileProvider(string pathPrefix, string contentRoot)
        {
            BasePath = NormalizePath(pathPrefix);
            InnerProvider = new PhysicalFileProvider(contentRoot);
        }

        public PhysicalFileProvider InnerProvider { get; }

        public PathString BasePath { get; }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var modifiedSub = NormalizePath(subpath);

            if (BasePath == "/")
            {
                return InnerProvider.GetDirectoryContents(modifiedSub);
            }

            if (StartsWithBasePath(modifiedSub, out var physicalPath))
            {
                return InnerProvider.GetDirectoryContents(physicalPath.Value);
            }
            else if (string.Equals(subpath, string.Empty) || string.Equals(modifiedSub, "/"))
            {
                return new StaticWebAssetsDirectoryRoot(BasePath);
            }
            else if (BasePath.StartsWithSegments(modifiedSub, FilePathComparison, out var remaining))
            {
                return new StaticWebAssetsDirectoryRoot(remaining);
            }

            return NotFoundDirectoryContents.Singleton;
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath)
        {
            var modifiedSub = NormalizePath(subpath);

            if (BasePath == "/")
            {
                return InnerProvider.GetFileInfo(subpath);
            }

            if (!StartsWithBasePath(modifiedSub, out var physicalPath))
            {
                return new NotFoundFileInfo(subpath);
            }
            else
            {
                return InnerProvider.GetFileInfo(physicalPath.Value);
            }
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter)
        {
            return InnerProvider.Watch(filter);
        }

        private static string NormalizePath(string path)
        {
            path = path.Replace('\\', '/');
            return path != null && path.StartsWith("/") ? path : "/" + path;
        }

        private bool StartsWithBasePath(string subpath, out PathString rest)
        {
            return new PathString(subpath).StartsWithSegments(BasePath, FilePathComparison, out rest);
        }

        private class StaticWebAssetsDirectoryRoot : IDirectoryContents
        {
            private readonly string _nextSegment;

            public StaticWebAssetsDirectoryRoot(PathString remainingPath)
            {
                // We MUST use the Value property here because it is unescaped.
                _nextSegment = remainingPath.Value.Split("/", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }

            public bool Exists => true;

            public IEnumerator<IFileInfo> GetEnumerator()
            {
                return GenerateEnum();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GenerateEnum();
            }

            private IEnumerator<IFileInfo> GenerateEnum()
            {
                return new[] { new StaticWebAssetsFileInfo(_nextSegment) }
                    .Cast<IFileInfo>().GetEnumerator();
            }

            private class StaticWebAssetsFileInfo : IFileInfo
            {
                public StaticWebAssetsFileInfo(string name)
                {
                    Name = name;
                }

                public bool Exists => true;

                public long Length => throw new NotImplementedException();

                public string PhysicalPath => throw new NotImplementedException();

                public DateTimeOffset LastModified => throw new NotImplementedException();

                public bool IsDirectory => true;

                public string Name { get; }

                public Stream CreateReadStream()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
