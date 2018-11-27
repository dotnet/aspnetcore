// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.Language;

namespace RazorPageExecutionInstrumentationWebSite
{
    public class TestRazorProjectFileSystem : FileProviderRazorProjectFileSystem
    {
        public TestRazorProjectFileSystem(IRazorViewEngineFileProviderAccessor fileProviderAccessor, IHostingEnvironment hostingEnvironment)
            : base(fileProviderAccessor, hostingEnvironment)
        {
        }

        public override RazorProjectItem GetItem(string path)
        {
            var item = (FileProviderRazorProjectItem)base.GetItem(path);
            return new TestRazorProjectItem(item);
        }

        private class TestRazorProjectItem : FileProviderRazorProjectItem
        {
            public TestRazorProjectItem(FileProviderRazorProjectItem projectItem)
                : base(projectItem.FileInfo, projectItem.BasePath, projectItem.FilePath, projectItem.RelativePhysicalPath)
            {
            }

            public override Stream Read()
            {
                // Normalize line endings to '\r\n' (CRLF). This removes core.autocrlf, core.eol, core.safecrlf, and
                // .gitattributes from the equation and treats "\r\n" and "\n" as equivalent. Does not handle
                // some line endings like "\r" but otherwise ensures checksums and line mappings are consistent.
                string text;
                using (var streamReader = new StreamReader(base.Read()))
                {
                    text = streamReader.ReadToEnd().Replace("\r", "").Replace("\n", "\r\n");
                }

                var bytes = Encoding.UTF8.GetBytes(text);
                return new MemoryStream(bytes);
            }
        }
    }
}