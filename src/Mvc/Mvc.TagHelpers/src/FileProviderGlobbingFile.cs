// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

internal sealed class FileProviderGlobbingFile : FileInfoBase
{
    private const char DirectorySeparatorChar = '/';

    public FileProviderGlobbingFile(IFileInfo fileInfo, DirectoryInfoBase parent)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        ArgumentNullException.ThrowIfNull(parent);

        Name = fileInfo.Name;
        ParentDirectory = parent;
        FullName = ParentDirectory.FullName + DirectorySeparatorChar + Name;
    }

    public override string FullName { get; }

    public override string Name { get; }

    public override DirectoryInfoBase ParentDirectory { get; }
}
