// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Internal.Common.FileProviders;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Blazor.Build.Core.FileSystem
{
    internal class ClientFileProvider : CompositeMountedFileProvider
    {
        public ClientFileProvider(string clientAssemblyPath, string webRootPath)
            : base(GetContents(clientAssemblyPath, webRootPath))
        {
        }

        private static (string, IFileProvider)[] GetContents(string clientAssemblyPath, string webRootPath)
        {
            var fileProviders = new List<(string, IFileProvider)>();

            // There must always be a client assembly, and we always supply a /_framework
            // directory containing everything needed to execute it
            if (!File.Exists(clientAssemblyPath))
            {
                throw new FileNotFoundException($"Could not find client assembly file at '{clientAssemblyPath}'.", clientAssemblyPath);
            }
            var frameworkFileProvider = new FrameworkFileProvider(clientAssemblyPath);
            fileProviders.Add(("/_framework", frameworkFileProvider));
            
            // The web root directory is optional. If it exists and contains /index.html, then
            // we will inject the relevant <script> tag and supply that file. Otherwise, we just
            // don't supply an /index.html file.
            if (TryCreateIndexHtmlFileProvider(
                webRootPath, clientAssemblyPath, frameworkFileProvider, out var indexHtmlFileProvider))
            {
                fileProviders.Add(("/", indexHtmlFileProvider));
            }

            return fileProviders.ToArray();
        }

        private static bool TryCreateIndexHtmlFileProvider(
            string webRootPath, string assemblyPath, IFileProvider frameworkFileProvider, out IFileProvider result)
        {
            if (!string.IsNullOrEmpty(webRootPath))
            {
                var path = Path.Combine(webRootPath, "index.html");
                if (File.Exists(path))
                {
                    var template = File.ReadAllText(path);
                    var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                    var binFiles = frameworkFileProvider.GetDirectoryContents("/_bin");
                    result = new IndexHtmlFileProvider(template, assemblyName, binFiles);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
