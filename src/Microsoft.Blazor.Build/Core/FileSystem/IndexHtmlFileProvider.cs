// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Internal.Common.FileProviders;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.FileProviders;
using System.Linq;

namespace Microsoft.Blazor.Build.Core.FileSystem
{
    internal class IndexHtmlFileProvider : InMemoryFileProvider
    {
        public IndexHtmlFileProvider(string htmlTemplate, string assemblyName, IEnumerable<IFileInfo> binFiles)
            : base(ComputeContents(htmlTemplate, assemblyName, binFiles))
        {
        }

        private static IEnumerable<(string, Stream)> ComputeContents(string htmlTemplate, string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            if (htmlTemplate != null)
            {
                var html = GetIndexHtmlContents(htmlTemplate, assemblyName, binFiles);
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                var htmlStream = new MemoryStream(htmlBytes);
                yield return ("/index.html", htmlStream);
            }
        }

        private static string GetIndexHtmlContents(string htmlTemplate, string assemblyName, IEnumerable<IFileInfo> binFiles)
        {
            // TODO: Consider parsing the HTML properly so for example we don't insert into
            // the wrong place if there was also '</body>' in a JavaScript string literal
            return htmlTemplate
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
