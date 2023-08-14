// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// Extends <see cref="IFileProvider"/> with the ability to resolve symlink targets of the provided directory.
/// </summary>
internal interface IFileProviderWithLinkInfo : IFileProvider
{
    /// <summary>
    /// Returns the link target of the <see cref="IFileProvider"/>'s root directory.
    /// </summary>
    /// <remarks>
    /// Effectively <see cref="FileSystemInfo.ResolveLinkTarget"/>.
    /// </remarks>
    IFileInfoWithLinkInfo? ResolveLinkTarget(bool returnFinalTarget);

    public new IFileInfoWithLinkInfo GetFileInfo(string subpath);
}
