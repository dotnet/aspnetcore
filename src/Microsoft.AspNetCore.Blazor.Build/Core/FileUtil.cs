// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using System.IO;

namespace Microsoft.AspNetCore.Blazor.Build.Core
{
    internal static class FileUtil
    {
        public static void WriteFileProviderToDisk(IFileProvider fileProvider, string outputDir, bool clean)
        {
            Directory.CreateDirectory(outputDir);

            if (clean)
            {
                CleanDirectory(outputDir);
            }

            WriteFileProviderToDisk(fileProvider, string.Empty, outputDir);
        }

        private static void WriteFileProviderToDisk(IFileProvider fileProvider, string subpath, string outputDir)
        {
            foreach (var item in fileProvider.GetDirectoryContents(subpath))
            {
                var itemOutputPath = Path.Combine(outputDir, item.Name);
                if (item.IsDirectory)
                {
                    Directory.CreateDirectory(itemOutputPath);
                    WriteFileProviderToDisk(fileProvider, item.PhysicalPath, itemOutputPath);
                }
                else
                {
                    using (var stream = item.CreateReadStream())
                    using (var fileStream = File.Open(itemOutputPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            }
        }

        private static void CleanDirectory(string path)
        {
            foreach (var itemName in Directory.GetFileSystemEntries(path))
            {
                var itemPath = Path.Combine(path, itemName);
                if (File.GetAttributes(itemPath).HasFlag(FileAttributes.Directory))
                {
                    Directory.Delete(itemPath, recursive: true);
                }
                else
                {
                    File.Delete(itemPath);
                }
            }
        }
    }
}
