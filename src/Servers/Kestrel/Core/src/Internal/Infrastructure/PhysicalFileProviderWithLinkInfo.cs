// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// A <see cref="PhysicalFileProvider"/> that returns <see cref="IFileInfoWithLinkInfo"/>, rather than <see cref="IFileInfo"/>.
/// </summary>
internal sealed class PhysicalFileProviderWithLinkInfo : IFileProviderWithLinkInfo
{
    private readonly PhysicalFileProvider _inner;

    public PhysicalFileProviderWithLinkInfo(string root, ExclusionFilters filters)
    {
        _inner = new PhysicalFileProvider(root, filters);
    }

    IDirectoryContents IFileProvider.GetDirectoryContents(string subpath) => _inner.GetDirectoryContents(subpath);
    IChangeToken IFileProvider.Watch(string filter) => _inner.Watch(filter);

    IFileInfo IFileProvider.GetFileInfo(string subpath)
    {
        return ((IFileProviderWithLinkInfo)this).GetFileInfo(subpath);
    }

    IFileInfoWithLinkInfo IFileProviderWithLinkInfo.GetFileInfo(string subpath)
    {
        var fileInfo = _inner.GetFileInfo(subpath);
        if (!fileInfo.Exists && Directory.Exists(fileInfo.PhysicalPath))
        {
            // https://github.com/dotnet/runtime/issues/36575
            fileInfo = new PhysicalDirectoryInfo(new DirectoryInfo(fileInfo.PhysicalPath));
        }
        return new FileInfoWithLinkInfo(fileInfo);
    }

    private sealed class FileInfoWithLinkInfo : IFileInfoWithLinkInfo
    {
        private readonly IFileInfo _inner; // Could be either a file or a directory

        public FileInfoWithLinkInfo(IFileInfo inner)
        {
            _inner = inner;
        }

        bool IFileInfo.Exists => _inner.Exists;
        bool IFileInfo.IsDirectory => _inner.IsDirectory;
        DateTimeOffset IFileInfo.LastModified => _inner.LastModified;
        long IFileInfo.Length => _inner.Length;
        string IFileInfo.Name => _inner.Name;
        string? IFileInfo.PhysicalPath => _inner.PhysicalPath;
        Stream IFileInfo.CreateReadStream() => _inner.CreateReadStream();

        IFileInfoWithLinkInfo? IFileInfoWithLinkInfo.ResolveLinkTarget(bool returnFinalTarget)
        {
            if (!_inner.Exists)
            {
                return null;
            }

            var path = _inner.PhysicalPath;

            if (path is null)
            {
                return null;
            }

            var fileSystemInfo = _inner.IsDirectory ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);

            var linkFileSystemInfo = fileSystemInfo.ResolveLinkTarget(returnFinalTarget);

            if (linkFileSystemInfo is FileInfo linkFileInfo)
            {
                return new FileInfoWithLinkInfo(new PhysicalFileInfo(linkFileInfo));
            }

            if (linkFileSystemInfo is DirectoryInfo linkDirInfo)
            {
                return new FileInfoWithLinkInfo(new PhysicalDirectoryInfo(linkDirInfo));
            }

            return null;
        }
    }
}
