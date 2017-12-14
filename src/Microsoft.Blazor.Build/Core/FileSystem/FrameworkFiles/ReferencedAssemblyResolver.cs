// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Linq;

namespace Microsoft.Blazor.Build.Core.FileSystem
{
    internal class ReferencedAssemblyResolver
    {
        private readonly IFileProvider _bcl;
        private readonly string _searchDirectory;

        public ReferencedAssemblyResolver(IFileProvider bcl, string searchDirectory)
        {
            _bcl = bcl;
            _searchDirectory = searchDirectory;
        }

        public bool TryResolve(string name, out byte[] assemblyBytes)
        {
            var filename = $"{name}.dll";
            if (SearchInFileProvider(_bcl, string.Empty, filename, out var fileInfo))
            {
                using (var ms = new MemoryStream())
                using (var fileInfoStream = fileInfo.CreateReadStream())
                {
                    fileInfoStream.CopyTo(ms);
                    assemblyBytes = ms.ToArray();
                    return true;
                }
            }
            else
            {
                var searchDirPath = Path.Combine(_searchDirectory, filename);
                if (File.Exists(searchDirPath))
                {
                    assemblyBytes = File.ReadAllBytes(searchDirPath);
                    return true;
                }
                else
                {
                    assemblyBytes = null;
                    return false;
                }
            }
        }

        private static bool SearchInFileProvider(IFileProvider fileProvider, string searchRootDirNoTrailingSlash, string name, out IFileInfo file)
        {
            var possibleFullPath = $"{searchRootDirNoTrailingSlash}/{name}";
            var possibleResult = fileProvider.GetFileInfo(possibleFullPath);
            if (possibleResult.Exists)
            {
                file = possibleResult;
                return true;
            }

            var childDirs = fileProvider.GetDirectoryContents(searchRootDirNoTrailingSlash)
                .Where(item => item.IsDirectory);
            foreach (var childDir in childDirs)
            {
                if (SearchInFileProvider(fileProvider, childDir.PhysicalPath, name, out file))
                {
                    return true;
                }
            }

            file = null;
            return false;
        }
    }
}
