// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    public class FileProviderGlobbingFile : FileInfoBase
    {
        private const char DirectorySeparatorChar = '/';

        public FileProviderGlobbingFile(IFileInfo fileInfo, DirectoryInfoBase parent)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            Name = fileInfo.Name;
            ParentDirectory = parent;
            FullName = ParentDirectory.FullName + DirectorySeparatorChar + Name;
        }

        public override string FullName { get; }

        public override string Name { get; }

        public override DirectoryInfoBase ParentDirectory { get; }
    }
}