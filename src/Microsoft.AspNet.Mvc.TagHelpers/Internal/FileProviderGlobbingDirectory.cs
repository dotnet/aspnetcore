// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.FileProviders;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    public class FileProviderGlobbingDirectory : DirectoryInfoBase
    {
        private const char DirectorySeparatorChar = '/';
        private readonly IFileProvider _fileProvider;
        private readonly IFileInfo _fileInfo;
        private readonly FileProviderGlobbingDirectory _parent;
        private readonly bool _isRoot;

        public FileProviderGlobbingDirectory(
            [NotNull] IFileProvider fileProvider,
            IFileInfo fileInfo,
            FileProviderGlobbingDirectory parent)
        {
            _fileProvider = fileProvider;
            _fileInfo = fileInfo;
            _parent = parent;

            if (_fileInfo == null)
            {
                // We're the root of the directory tree
                RelativePath = string.Empty;
                _isRoot = true;
            }
            else if (!string.IsNullOrEmpty(parent?.RelativePath))
            {
                // We have a parent and they have a relative path so concat that with my name
                RelativePath = _parent.RelativePath + DirectorySeparatorChar + _fileInfo.Name;
            }
            else
            {
                // We have a parent which is the root, so just use my name
                RelativePath = _fileInfo.Name;
            }
        }

        public string RelativePath { get; }

        public override string FullName
        {
            get
            {
                if (_isRoot)
                {
                    // We're the root, so just use our name
                    return Name;
                }
                
                return _parent.FullName + DirectorySeparatorChar + Name;
            }
        }

        public override string Name
        {
            get
            {
                return _fileInfo?.Name;
            }
        }

        public override DirectoryInfoBase ParentDirectory
        {
            get
            {
                return _parent;
            }
        }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos(
            string searchPattern,
            SearchOption searchOption)
        {
            if (!string.Equals(searchPattern, "*", StringComparison.OrdinalIgnoreCase))
            {
                // Only * based searches are ever performed against this API and we have an item to change this API
                // such that the searchPattern doesn't even get passed in, so this is just a safe-guard until then.
                // The searchPattern here has no relation to the globbing pattern.
                throw new ArgumentException(
                    "Only full wildcard searches are supported, i.e. \"*\".",
                    nameof(searchPattern));
            }

            if (searchOption != SearchOption.TopDirectoryOnly)
            {
                // Only SearchOption.TopDirectoryOnly is actually used in the implementation of Matcher and will likely
                // be removed from DirectoryInfoBase in the near future. This is just a safe-guard until then.
                // The searchOption here has no relation to the globbing pattern.
                throw new ArgumentException(
                    $"Only {nameof(SearchOption.TopDirectoryOnly)} is supported.",
                    nameof(searchOption));
            }



            foreach (var fileInfo in _fileProvider.GetDirectoryContents(RelativePath))
            {
                yield return BuildFileResult(fileInfo);
            }
        }

        public override DirectoryInfoBase GetDirectory(string path)
        {
            return new FileProviderGlobbingDirectory(_fileProvider, _fileProvider.GetFileInfo(path), this);
        }

        public override FileInfoBase GetFile(string path)
        {
            return new FileProviderGlobbingFile(_fileProvider.GetFileInfo(path), this);
        }

        private FileSystemInfoBase BuildFileResult(IFileInfo fileInfo)
        {
            if (fileInfo.IsDirectory)
            {
                return new FileProviderGlobbingDirectory(_fileProvider, fileInfo, this);
            }

            return new FileProviderGlobbingFile(fileInfo, this);
        }
    }
}