// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Blazor.Internal.Common.FileProviders
{
    public class InMemoryFileProvider : IFileProvider
    {
        // Since the IFileProvider APIs don't include any way of asking for parent or
        // child directories, it's efficient to store everything by full path
        private readonly IDictionary<string, IFileInfo> _filesByFullPath;
        private readonly IDictionary<string, InMemoryDirectoryContents> _directoriesByFullPath;

        // It's convenient to use forward slash, because it matches URL conventions
        public const string DirectorySeparatorChar = "/";

        public InMemoryFileProvider(IEnumerable<(string, byte[])> contents) : this(
            contents.Select(pair => InMemoryFileInfo
                .ForExistingFile(pair.Item1, pair.Item2, DateTime.Now)))
        {
        }

        public InMemoryFileProvider(IEnumerable<IFileInfo> contents)
        {
            _filesByFullPath = contents
               .ToDictionary(
                   fileInfo => fileInfo.PhysicalPath,
                   fileInfo => (IFileInfo)fileInfo);

            _directoriesByFullPath = _filesByFullPath.Values
                .GroupBy(file => GetDirectoryName(file.PhysicalPath))
                .ToDictionary(
                    group => group.Key,
                    group => new InMemoryDirectoryContents(group));

            foreach (var dirToInsert in _directoriesByFullPath.Keys.ToList())
            {
                AddSubdirectoryEntry(dirToInsert);
            }
        }

        private void AddSubdirectoryEntry(string dirPath)
        {
            // If this is the root directory, there's no parent
            if (dirPath.Length == 0)
            {
                return;
            }

            // Ensure parent directory exists
            var parentDirPath = GetDirectoryName(dirPath);
            if (!_directoriesByFullPath.ContainsKey(parentDirPath))
            {
                _directoriesByFullPath.Add(
                    parentDirPath,
                    new InMemoryDirectoryContents(Enumerable.Empty<IFileInfo>()));
            }

            var parentDir = _directoriesByFullPath[parentDirPath];
            if (!parentDir.ContainsName(Path.GetFileName(dirPath)))
            {
                parentDir.AddItem(
                    InMemoryFileInfo.ForExistingDirectory(dirPath));
            }

            // Doing this recursively creates all ancestor directories
            AddSubdirectoryEntry(parentDirPath);
        }

        private static string GetDirectoryName(string fullPath)
            => fullPath.Substring(0, fullPath.LastIndexOf(DirectorySeparatorChar));

        public IDirectoryContents GetDirectoryContents(string subpath)
            => _directoriesByFullPath.TryGetValue(StripTrailingSeparator(subpath), out var result)
                ? result
                : new InMemoryDirectoryContents(null);

        public IFileInfo GetFileInfo(string subpath)
            => _filesByFullPath.TryGetValue(subpath, out var fileInfo)
                ? fileInfo
                : InMemoryFileInfo.ForNonExistingFile(subpath);

        public IChangeToken Watch(string filter)
            => throw new NotImplementedException();

        private string StripTrailingSeparator(string path) => path.EndsWith(DirectorySeparatorChar)
            ? path.Substring(0, path.Length - 1)
            : path;
    }
}
