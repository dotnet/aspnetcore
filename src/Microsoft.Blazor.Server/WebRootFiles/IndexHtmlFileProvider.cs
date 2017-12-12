// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Internal.Common.FileProviders;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.FileProviders;
using System.Linq;

namespace Microsoft.Blazor.Server.WebRootFiles
{
    internal class IndexHtmlFileProvider : InMemoryFileProvider
    {
        public IndexHtmlFileProvider(string clientWebRoot, string assemblyName, IEnumerable<IFileInfo> binFiles)
            : base(ComputeContents(clientWebRoot, assemblyName, binFiles))
        {
        }

        private static IEnumerable<(string, Stream)> ComputeContents(string clientWebRoot, string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            var html = GetIndexHtmlContents(clientWebRoot, assemblyName, binFiles);
            if (html != null)
            {
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                var htmlStream = new MemoryStream(htmlBytes);
                yield return ("/index.html", htmlStream);
            }
        }

        private static string GetIndexHtmlContents(string clientWebRoot, string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            var indexHtmlPath = Path.Combine(clientWebRoot, "index.html");
            if (!File.Exists(indexHtmlPath))
            {
                return null;
            }

            // TODO: Consider parsing the HTML properly so for example we don't insert into
            // the wrong place if there was also '</body>' in a JavaScript string literal
            return File.ReadAllText(indexHtmlPath)
                .Replace("</body>", CreateBootMarkup(assemblyName, binFiles) + "\n</body>");
        }

        private static string CreateBootMarkup(string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            var assemblyNameWithExtension = $"{assemblyName}.dll";
            var referenceNames = binFiles
                .Where(file => !string.Equals(file.Name, assemblyNameWithExtension))
                .Select(file => file.Name);
            var referencesAttribute = string.Join(',', referenceNames.ToArray());

            return $"<script src=\"/_framework/blazor.js\"" +
                   $" main=\"{assemblyNameWithExtension}\"" +
                   $" references=\"{referencesAttribute}\"></script>";
        }
    }
}
