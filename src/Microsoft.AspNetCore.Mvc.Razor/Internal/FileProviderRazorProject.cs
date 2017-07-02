// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class FileProviderRazorProject : RazorProject
    {
        private const string RazorFileExtension = ".cshtml";
        private readonly IFileProvider _provider;

        public FileProviderRazorProject(IRazorViewEngineFileProviderAccessor accessor)
        {
            if (accessor == null)
            {
                throw new ArgumentNullException(nameof(accessor));
            }

            _provider = accessor.FileProvider;
        }

        public override RazorProjectItem GetItem(string path)
        {
            path = NormalizeAndEnsureValidPath(path);
            var fileInfo = _provider.GetFileInfo(path);
            return new FileProviderRazorProjectItem(fileInfo, basePath: string.Empty, path: path);
        }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string path)
        {
            path = NormalizeAndEnsureValidPath(path);
            return EnumerateFiles(_provider.GetDirectoryContents(path), path, prefix: string.Empty);
        }

        private IEnumerable<RazorProjectItem> EnumerateFiles(IDirectoryContents directory, string basePath, string prefix)
        {
            if (directory.Exists)
            {
                foreach (var file in directory)
                {
                    if (file.IsDirectory)
                    {
                        var relativePath = prefix + "/" + file.Name;
                        var subDirectory = _provider.GetDirectoryContents(JoinPath(basePath, relativePath));
                        var children = EnumerateFiles(subDirectory, basePath, relativePath);
                        foreach (var child in children)
                        {
                            yield return child;
                        }
                    }
                    else if (string.Equals(RazorFileExtension, Path.GetExtension(file.Name), StringComparison.OrdinalIgnoreCase))
                    {
                        yield return new FileProviderRazorProjectItem(file, basePath, prefix + "/" + file.Name);
                    }
                }
            }
        }

        private static string JoinPath(string path1, string path2)
        {
            var hasTrailingSlash = path1.EndsWith("/", StringComparison.Ordinal);
            var hasLeadingSlash = path2.StartsWith("/", StringComparison.Ordinal);
            if (hasLeadingSlash && hasTrailingSlash)
            {
                return path1 + path2.Substring(1);
            }
            else if (hasLeadingSlash || hasTrailingSlash)
            {
                return path1 + path2;
            }

            return path1 + "/" + path2;
        }
    }
}
