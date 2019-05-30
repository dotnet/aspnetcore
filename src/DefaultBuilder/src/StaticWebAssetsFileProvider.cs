// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore
{
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
