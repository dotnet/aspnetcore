// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a file from disk to the response using mechanisms provided
    /// by the host.
    /// </summary>
    public class FilePathResult : FileResult
    {
        private const int DefaultBufferSize = 0x1000;

        /// <summary>
        /// Creates a new <see cref="FilePathResult"/> instance with
        /// the provided <paramref name="fileName"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute
        /// path. Relative and virtual paths are not supported.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FilePathResult([NotNull] string fileName, [NotNull] string contentType)
            : base(contentType)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Creates a new <see cref="FilePathResult"/> instance with
        /// the provided <paramref name="fileName"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute
        /// path. Relative and virtual paths are not supported.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FilePathResult(
            [NotNull] string fileName,
            [NotNull] string contentType,
            [NotNull] IFileSystem fileSystem)
            : base(contentType)
        {
            FileName = fileName;
            FileSystem = fileSystem;
        }

        /// <summary>
        /// Gets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the <see cref="IFileSystem"/> used to resolve paths.
        /// </summary>
        public IFileSystem FileSystem { get; private set; }

        /// <inheritdoc />
        protected override Task WriteFileAsync(HttpResponse response, CancellationToken cancellation)
        {
            var sendFile = response.HttpContext.GetFeature<IHttpSendFileFeature>();

            var fileSystem = GetFileSystem(response.HttpContext.RequestServices);

            var filePath = ResolveFilePath(fileSystem);

            if (sendFile != null)
            {
                return sendFile.SendFileAsync(
                    filePath,
                    offset: 0,
                    length: null,
                    cancellation: cancellation);
            }
            else
            {
                return CopyStreamToResponse(filePath, response, cancellation);
            }
        }

        internal string ResolveFilePath(IFileSystem fileSystem)
        {
            // Let the file system try to get the file and if it can't,
            // fallback to trying the path directly unless the path starts with '/'.
            // In that case we consider it relative and won't try to resolve it as
            // a full path

            var path = NormalizePath(FileName);

            if (IsPathRooted(path))
            {
                // The path is absolute
                // C:\...\file.ext
                // C:/.../file.ext
                return path;
            }

            var fileInfo = fileSystem.GetFileInfo(path);
            if (fileInfo.Exists)
            {
                // The path is relative and IFileSystem found the file, so return the full
                // path.
                return fileInfo.PhysicalPath;
            }

            // We are absolutely sure the path is relative, and couldn't find the file
            // on the file system.
            var message = Resources.FormatFileResult_InvalidPath(path);
            throw new FileNotFoundException(message, path);
        }

        /// <summary>
        /// Creates a normalized representation of the given <paramref name="path"/>. The default
        /// implementation doesn't support files with '\' in the file name and treats the '\' as
        /// a directory separator. The default implementation will convert all the '\' into '/'
        /// and will remove leading '~' characters.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        // Internal for unit testing purposes only
        protected internal virtual string NormalizePath([NotNull] string path)
        {
            // Unix systems support '\' as part of the file name. So '\' is not
            // a valid directory separator in those systems. Here we make the conscious
            // choice of replacing '\' for '/' which means that file names with '\' will
            // not be supported.

            if (path.StartsWith("~/", StringComparison.Ordinal))
            {
                // We don't support virtual paths for now, so we just treat them as relative
                // paths.
                return path.Substring(1).Replace('\\', '/');
            }

            if (path.StartsWith("~\\", StringComparison.Ordinal))
            {
                // ~\ is not a valid virtual path, and we don't want to replace '\' with '/' as it
                // ofuscates the error, so just return the original path and throw at a later point
                // when we can't find the file.
                return path;
            }

            return path.Replace('\\', '/');
        }

        /// <summary>
        /// Determines if the provided path is absolute or relative. The default implementation considers
        /// paths starting with '/' to be relative.
        /// </summary>
        /// <param name="path">The path to examine.</param>
        /// <returns>True if the path is absolute.</returns>
        // Internal for unit testing purposes only
        protected internal virtual bool IsPathRooted([NotNull] string path)
        {
            // We consider paths to be rooted if they start with '<<VolumeLetter>>:' and do
            // not start with '\' or '/'. In those cases, even that the paths are 'traditionally'
            // rooted, we consider them to be relative.
            // In Unix rooted paths start with '/' which is not supported by this action result
            // by default.

            return Path.IsPathRooted(path) && (IsNetworkPath(path) || !StartsWithForwardOrBackSlash(path));
        }

        private static bool StartsWithForwardOrBackSlash(string path)
        {
            return path.StartsWith("/", StringComparison.Ordinal) ||
                path.StartsWith("\\", StringComparison.Ordinal);
        }

        private static bool IsNetworkPath(string path)
        {
            return path.StartsWith("//", StringComparison.Ordinal) ||
                path.StartsWith("\\\\", StringComparison.Ordinal);
        }

        private IFileSystem GetFileSystem(IServiceProvider requestServices)
        {
            if (FileSystem != null)
            {
                return FileSystem;
            }

            // For right now until we can use IWebRootFileSystemProvider, see
            // https://github.com/aspnet/Hosting/issues/86 for details.
            var hostingEnvironment = requestServices.GetService<IHostingEnvironment>();
            FileSystem = new PhysicalFileSystem(hostingEnvironment.WebRoot);

            return FileSystem;
        }

        private static async Task CopyStreamToResponse(
            string fileName,
            HttpResponse response,
            CancellationToken cancellation)
        {
            var fileStream = new FileStream(
                fileName, FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                DefaultBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using (fileStream)
            {
                await fileStream.CopyToAsync(response.Body, DefaultBufferSize, cancellation);
            }
        }
    }
}