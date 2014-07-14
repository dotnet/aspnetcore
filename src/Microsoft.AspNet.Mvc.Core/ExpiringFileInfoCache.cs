// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.AspNet.FileSystems;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Core
{
    /// <summary>
    /// A default implementation for the <see cref="IFileInfoCache" interface./>
    /// </summary>
    public class ExpiringFileInfoCache : IFileInfoCache
    {
        private readonly ConcurrentDictionary<string, ExpiringFileInfo> _fileInfoCache =
            new ConcurrentDictionary<string, ExpiringFileInfo>(StringComparer.Ordinal);

        private readonly PhysicalFileSystem _fileSystem;
        private readonly TimeSpan _offset;

        protected virtual IFileSystem FileSystem
        {
            get
            {
                return _fileSystem;
            }
        }

        protected virtual DateTime UtcNow
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        public ExpiringFileInfoCache(IApplicationEnvironment env,
                                     IOptionsAccessor<MvcOptions> optionsAccessor)
        {
            // TODO: Inject the IFileSystem but only when we get it from the host
            _fileSystem = new PhysicalFileSystem(env.ApplicationBasePath);
            _offset = optionsAccessor.Options.ViewEngineOptions.ExpirationBeforeCheckingFilesOnDisk;
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo([NotNull] string virtualPath)
        {
            IFileInfo fileInfo;
            ExpiringFileInfo expiringFileInfo;

            var utcNow = UtcNow;

            if (_fileInfoCache.TryGetValue(virtualPath, out expiringFileInfo)
                && expiringFileInfo.ValidUntil > utcNow)
            {
                fileInfo = expiringFileInfo.FileInfo;
            }
            else
            {
                FileSystem.TryGetFileInfo(virtualPath, out fileInfo);

                expiringFileInfo = new ExpiringFileInfo()
                {
                    FileInfo = fileInfo,
                    ValidUntil = _offset == TimeSpan.MaxValue ? DateTime.MaxValue : utcNow.Add(_offset),
                };

                _fileInfoCache.AddOrUpdate(virtualPath, expiringFileInfo, (a, b) => expiringFileInfo);
            }

            return fileInfo;
        }

        private class ExpiringFileInfo
        {
            public IFileInfo FileInfo { get; set; }
            public DateTime ValidUntil { get; set; }
        }
    }
}