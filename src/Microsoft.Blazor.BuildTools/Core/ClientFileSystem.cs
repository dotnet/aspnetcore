// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.BuildTools.Core.FrameworkFiles;
using Microsoft.Blazor.BuildTools.Core.WebRootFiles;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Blazor.BuildTools.Core
{
    internal static class ClientFileSystem
    {
        public static IFileProvider Instantiate(string clientAssemblyPath, string webRootPath)
        {
            var fileProviders = new List<IFileProvider>();

            if (!File.Exists(clientAssemblyPath))
            {
                throw new FileNotFoundException($"Could not find client assembly file at '{clientAssemblyPath}'.", clientAssemblyPath);
            }

            var frameworkFileProvider = FrameworkFileProvider.Instantiate(
                clientAssemblyPath);
            fileProviders.Add(frameworkFileProvider);

            if (!string.IsNullOrEmpty(webRootPath))
            {
                if (!Directory.Exists(webRootPath))
                {
                    throw new DirectoryNotFoundException($"Could not find web root directory at '{webRootPath}'.");
                }

                var webRootFileProvider = WebRootFileProvider.Instantiate(
                    webRootPath,
                    Path.GetFileNameWithoutExtension(clientAssemblyPath),
                    frameworkFileProvider.GetDirectoryContents("/_bin"));
                fileProviders.Add(webRootFileProvider);
            }

            return new CompositeFileProvider(fileProviders);
        }
    }
}
