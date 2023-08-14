// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// Extends <see cref="IFileInfo"/> with the ability to resolve symlink targets.
/// </summary>
internal interface IFileInfoWithLinkInfo : IFileInfo
{
    /// <summary>
    /// <see cref="IFileInfo"/> equivalent of <see cref="FileSystemInfo.ResolveLinkTarget"/>.
    /// </summary>
    IFileInfoWithLinkInfo? ResolveLinkTarget(bool returnFinalTarget);
}
