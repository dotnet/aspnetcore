// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore
{
    /// <summary>
    /// A <see cref="IFileProvider"/> for serving static web assets during development.
    /// </summary>
    internal class StaticWebAssetsFileProvider : IFileProvider
    {
        private static readonly StringComparison FileSystemBasePathComparisonMode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            StringComparison.OrdinalIgnoreCase :
            StringComparison.Ordinal;

        /// <summary>
        /// Initializes a new instance of <see cref="StaticWebAssetsFileProvider"/>.
        /// </summary>
        /// <param name="pathPrefix">The path prefix under which the files in the <paramref name="contentRoot"/> folder will
        /// be mapped.</param>
        /// <param name="contentRoot">The absolute path to the content root associated with the static web assets.</param>
        public StaticWebAssetsFileProvider(string pathPrefix, string contentRoot)
        {
            BasePath = pathPrefix.StartsWith("/") ? pathPrefix : "/" + pathPrefix;
            InnerProvider = new PhysicalFileProvider(contentRoot);
        }

        public PhysicalFileProvider InnerProvider { get; }

        public string BasePath { get; }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (!subpath.StartsWith(BasePath, FileSystemBasePathComparisonMode))
            {
                return NotFoundDirectoryContents.Singleton;
            }
            else
            {
                return InnerProvider.GetDirectoryContents(subpath.Substring(BasePath.Length));
            }
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath)
        {
            if (!subpath.StartsWith(BasePath, FileSystemBasePathComparisonMode))
            {
                return new NotFoundFileInfo(subpath);
            }
            else
            {
                return InnerProvider.GetFileInfo(subpath.Substring(BasePath.Length));
            }
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter)
        {
            return InnerProvider.Watch(filter);
        }
    }
}
