// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            BasePath = new PathString(pathPrefix.StartsWith("/") ? pathPrefix : "/" + pathPrefix);
            InnerProvider = new PhysicalFileProvider(contentRoot);
        }

        public PhysicalFileProvider InnerProvider { get; }

        public PathString BasePath { get; }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (!StartsWithBasePath(subpath, out var physicalPath))
            {
                return NotFoundDirectoryContents.Singleton;
            }
            else
            {
                return InnerProvider.GetDirectoryContents(physicalPath);
            }
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath)
        {
            if (!StartsWithBasePath(subpath, out var physicalPath))
            {
                return new NotFoundFileInfo(subpath);
            }
            else
            {
                return InnerProvider.GetFileInfo(physicalPath);
            }
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter)
        {
            return InnerProvider.Watch(filter);
        }

        private bool StartsWithBasePath(string subpath, out PathString rest)
        {
            return new PathString(subpath).StartsWithSegments(BasePath, FilePathComparison, out rest);
        }
    }
}
