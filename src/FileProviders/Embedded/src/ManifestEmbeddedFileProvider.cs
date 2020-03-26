// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders.Embedded.Manifest;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// An embedded file provider that uses a manifest compiled in the assembly to
    /// reconstruct the original paths of the embedded files when they were embedded
    /// into the assembly.
    /// </summary>
    public class ManifestEmbeddedFileProvider : IFileProvider
    {
        private readonly DateTimeOffset _lastModified;

        /// <summary>
        /// Initializes a new instance of <see cref="ManifestEmbeddedFileProvider"/>.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded files.</param>
        public ManifestEmbeddedFileProvider(Assembly assembly)
            : this(assembly, ManifestParser.Parse(assembly), ResolveLastModified(assembly)) { }

        /// <summary>
        /// Initializes a new instance of <see cref="ManifestEmbeddedFileProvider"/>.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded files.</param>
        /// <param name="root">The relative path from the root of the manifest to use as root for the provider.</param>
        public ManifestEmbeddedFileProvider(Assembly assembly, string root)
            : this(assembly, root, ResolveLastModified(assembly))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ManifestEmbeddedFileProvider"/>.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded files.</param>
        /// <param name="root">The relative path from the root of the manifest to use as root for the provider.</param>
        /// <param name="lastModified">The LastModified date to use on the <see cref="IFileInfo"/> instances
        /// returned by this <see cref="IFileProvider"/>.</param>
        public ManifestEmbeddedFileProvider(Assembly assembly, string root, DateTimeOffset lastModified)
            : this(assembly, ManifestParser.Parse(assembly).Scope(root), lastModified)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ManifestEmbeddedFileProvider"/>.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded files.</param>
        /// <param name="root">The relative path from the root of the manifest to use as root for the provider.</param>
        /// <param name="manifestName">The name of the embedded resource containing the manifest.</param>
        /// <param name="lastModified">The LastModified date to use on the <see cref="IFileInfo"/> instances
        /// returned by this <see cref="IFileProvider"/>.</param>
        public ManifestEmbeddedFileProvider(Assembly assembly, string root, string manifestName, DateTimeOffset lastModified)
            : this(assembly, ManifestParser.Parse(assembly, manifestName).Scope(root), lastModified)
        {
        }

        internal ManifestEmbeddedFileProvider(Assembly assembly, EmbeddedFilesManifest manifest, DateTimeOffset lastModified)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            Assembly = assembly;
            Manifest = manifest;
            _lastModified = lastModified;
        }

        /// <summary>
        /// Gets the <see cref="Assembly"/> for this provider.
        /// </summary>
        public Assembly Assembly { get; }

        internal EmbeddedFilesManifest Manifest { get; }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var entry = Manifest.ResolveEntry(subpath);
            if (entry == null || entry == ManifestEntry.UnknownPath)
            {
                return NotFoundDirectoryContents.Singleton;
            }

            if (!(entry is ManifestDirectory directory))
            {
                return NotFoundDirectoryContents.Singleton;
            }

            return new ManifestDirectoryContents(Assembly, directory, _lastModified);
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath)
        {
            var entry = Manifest.ResolveEntry(subpath);
            switch (entry)
            {
                case null:
                    return new NotFoundFileInfo(subpath);
                case ManifestFile f:
                    return new ManifestFileInfo(Assembly, f, _lastModified);
                case ManifestDirectory d when d != ManifestEntry.UnknownPath:
                    return new NotFoundFileInfo(d.Name);
            }

            return new NotFoundFileInfo(subpath);
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return NullChangeToken.Singleton;
        }

        private static DateTimeOffset ResolveLastModified(Assembly assembly)
        {
            var result = DateTimeOffset.UtcNow;

            if (!string.IsNullOrEmpty(assembly.Location))
            {
                try
                {
                    result = File.GetLastWriteTimeUtc(assembly.Location);
                }
                catch (PathTooLongException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return result;
        }
    }
}
