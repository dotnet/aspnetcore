// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// An entry in <see cref="ICompilerCache"/> that contain metadata about precompiled and dynamically compiled file.
    /// </summary>
    public class CompilerCacheEntry
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheEntry"/> for a file that was precompiled.
        /// </summary>
        /// <param name="info">Metadata about the precompiled file.</param>
        /// <param name="compiledType">The compiled <see cref="Type"/>.</param>
        public CompilerCacheEntry([NotNull] RazorFileInfo info, [NotNull] Type compiledType)
        {
            CompiledType = compiledType;
            RelativePath = info.RelativePath;
            Length = info.Length;
            LastModified = info.LastModified;
            Hash = info.Hash;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheEntry"/> for a file that was dynamically compiled.
        /// </summary>
        /// <param name="info">Metadata about the file that was compiled.</param>
        /// <param name="compiledType">The compiled <see cref="Type"/>.</param>
        public CompilerCacheEntry([NotNull] RelativeFileInfo info, [NotNull] Type compiledType)
        {
            CompiledType = compiledType;
            RelativePath = info.RelativePath;
            Length = info.FileInfo.Length;
            LastModified = info.FileInfo.LastModified;
        }

        /// <summary>
        /// Gets the <see cref="Type"/> produced as a result of compilation.
        /// </summary>
        public Type CompiledType { get; private set; }

        /// <summary>
        /// Gets the path of the compiled file relative to the root of the application.
        /// </summary>
        public string RelativePath { get; private set; }

        /// <summary>
        /// Gets the size of file (in bytes) on disk.
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// Gets or sets the last modified <see cref="DateTimeOffset"/> for the file at the time of compilation.
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// Gets the file hash, should only be available for pre compiled files.
        /// </summary>
        public string Hash { get; private set; }

        /// <summary>
        /// Gets a flag that indicates if the file is precompiled.
        /// </summary>
        public bool IsPreCompiled
        {
            get
            {
                return Hash != null;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CompilerCacheEntry"/> for the nearest ViewStart that the compiled type
        /// depends on.
        /// </summary>
        public CompilerCacheEntry AssociatedViewStartEntry { get; set; }
    }
}
