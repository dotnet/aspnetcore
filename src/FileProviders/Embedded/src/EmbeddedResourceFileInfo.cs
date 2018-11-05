// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.FileProviders.Embedded
{
    /// <summary>
    /// Represents a file embedded in an assembly.
    /// </summary>
    public class EmbeddedResourceFileInfo : IFileInfo
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePath;

        private long? _length;

        /// <summary>
        /// Initializes a new instance of <see cref="EmbeddedFileProvider"/> for an assembly using <paramref name="resourcePath"/> as the base
        /// </summary>
        /// <param name="assembly">The assembly that contains the embedded resource</param>
        /// <param name="resourcePath">The path to the embedded resource</param>
        /// <param name="name">An arbitrary name for this instance</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset" /> to use for <see cref="LastModified" /></param>
        public EmbeddedResourceFileInfo(
            Assembly assembly,
            string resourcePath,
            string name,
            DateTimeOffset lastModified)
        {
            _assembly = assembly;
            _resourcePath = resourcePath;
            Name = name;
            LastModified = lastModified;
        }

        /// <summary>
        /// Always true.
        /// </summary>
        public bool Exists => true;

        /// <summary>
        /// The length, in bytes, of the embedded resource
        /// </summary>
        public long Length
        {
            get
            {
                if (!_length.HasValue)
                {
                    using (var stream = _assembly.GetManifestResourceStream(_resourcePath))
                    {
                        _length = stream.Length;
                    }
                }
                return _length.Value;
            }
        }

        /// <summary>
        /// Always null.
        /// </summary>
        public string PhysicalPath => null;

        /// <summary>
        /// The name of embedded file
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The time, in UTC, when the <see cref="EmbeddedFileProvider"/> was created
        /// </summary>
        public DateTimeOffset LastModified { get; }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsDirectory => false;

        /// <inheritdoc />
        public Stream CreateReadStream()
        {
            var stream = _assembly.GetManifestResourceStream(_resourcePath);
            if (!_length.HasValue)
            {
                _length = stream.Length;
            }

            return stream;
        }
    }
}
