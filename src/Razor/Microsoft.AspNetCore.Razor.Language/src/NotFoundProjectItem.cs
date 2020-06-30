// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// A <see cref="RazorProjectItem"/> that does not exist.
    /// </summary>
    internal class NotFoundProjectItem : RazorProjectItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NotFoundProjectItem"/>.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <param name="path">The path.</param>
        /// <param name="fileKind">The file kind</param>
        public NotFoundProjectItem(string basePath, string path, string fileKind)
        {
            BasePath = basePath;
            FilePath = path;
            FileKind = fileKind ?? FileKinds.GetFileKindFromFilePath(path);
        }

        /// <inheritdoc />
        public override string BasePath { get; }

        /// <inheritdoc />
        public override string FilePath { get; }

        /// <inheritdoc />
        public override string FileKind { get; }

        /// <inheritdoc />
        public override bool Exists => false;

        /// <inheritdoc />
        public override string PhysicalPath => throw new NotSupportedException();

        /// <inheritdoc />
        public override Stream Read() => throw new NotSupportedException();
    }
}
