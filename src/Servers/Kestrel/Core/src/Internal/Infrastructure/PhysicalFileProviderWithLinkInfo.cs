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
        return new FileInfoWithLinkInfo(_inner.GetFileInfo(subpath));
    }

    IFileInfoWithLinkInfo? IFileProviderWithLinkInfo.ResolveLinkTarget(bool returnFinalTarget)
    {
        IFileInfoWithLinkInfo dirInfo = new DirectoryInfoWithLinkInfo(new DirectoryInfo(_inner.Root));
        return dirInfo.ResolveLinkTarget(returnFinalTarget);
    }

    private sealed class FileInfoWithLinkInfo : IFileInfoWithLinkInfo
    {
        private readonly IFileInfo _inner;

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

            var fileInfo = new FileInfo(path);

            var linkFileSystemInfo = fileInfo.ResolveLinkTarget(returnFinalTarget);

            if (linkFileSystemInfo is not FileInfo linkInfo)
            {
                return null;
            }

            return new FileInfoWithLinkInfo(new PhysicalFileInfo(linkInfo));
        }
    }
    private sealed class DirectoryInfoWithLinkInfo : IFileInfoWithLinkInfo
    {
        private readonly DirectoryInfo _directoryInfo;

        public DirectoryInfoWithLinkInfo(DirectoryInfo directoryInfo)
        {
            _directoryInfo = directoryInfo;
        }

        bool IFileInfo.IsDirectory => true;

        bool IFileInfo.Exists => _directoryInfo.Exists;
        DateTimeOffset IFileInfo.LastModified => _directoryInfo.LastWriteTimeUtc;
        string IFileInfo.Name => _directoryInfo.Name;
        string? IFileInfo.PhysicalPath => _directoryInfo.FullName;

        long IFileInfo.Length => throw new NotSupportedException(); // We could probably just return 0, though that's not strictly accurate
        Stream IFileInfo.CreateReadStream() => throw new NotSupportedException();

        IFileInfoWithLinkInfo? IFileInfoWithLinkInfo.ResolveLinkTarget(bool returnFinalTarget)
        {
            if (!_directoryInfo.Exists)
            {
                return null;
            }

            var linkFileSystemInfo = _directoryInfo.ResolveLinkTarget(returnFinalTarget);

            if (linkFileSystemInfo is not DirectoryInfo linkInfo)
            {
                return null;
            }

            return new DirectoryInfoWithLinkInfo(linkInfo);
        }
    }
}
