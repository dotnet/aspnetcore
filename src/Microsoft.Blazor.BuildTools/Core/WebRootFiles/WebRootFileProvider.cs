// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Blazor.BuildTools.Core.WebRootFiles
{
    internal static class WebRootFileProvider
    {
        public static IFileProvider Instantiate(
            string clientWebRoot, string assemblyName, IEnumerable<IFileInfo> binFiles)
            => new CompositeFileProvider(
                new IndexHtmlFileProvider(
                    ReadIndexHtmlFile(clientWebRoot), assemblyName, binFiles),
                new PhysicalFileProvider(clientWebRoot));

        private static string ReadIndexHtmlFile(string clientWebRoot)
        {
            var path = Path.Combine(clientWebRoot, "index.html");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}
