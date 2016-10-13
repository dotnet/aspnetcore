// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TestRazorSourceDocument : DefaultRazorSourceDocument
    {
        public static RazorSourceDocument CreateResource(string path, Encoding encoding = null)
        {
            var file = TestFile.Create(path);

            var stream = new MemoryStream();
            using (var input = file.OpenRead())
            {
                input.CopyTo(stream);
            }

            stream.Seek(0L, SeekOrigin.Begin);

            return new TestRazorSourceDocument(stream, encoding ?? Encoding.UTF8, path);
        }

        public static MemoryStream CreateContent(string content = "Hello, World!", Encoding encoding = null)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
            {
                writer.Write(content);
            }

            stream.Seek(0L, SeekOrigin.Begin);

            return stream;
        }

        public static RazorSourceDocument Create(string content = "Hello, world!", Encoding encoding = null)
        {
            var stream = CreateContent(content, encoding);
            return new TestRazorSourceDocument(stream, encoding ?? Encoding.UTF8, "test.cshtml");
        }

        public TestRazorSourceDocument(MemoryStream stream, Encoding encoding, string filename) 
            : base(stream, encoding, filename)
        {
        }
    }
}
