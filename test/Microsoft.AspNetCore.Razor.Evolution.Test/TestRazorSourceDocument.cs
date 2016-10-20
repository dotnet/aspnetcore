// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TestRazorSourceDocument : DefaultRazorSourceDocument
    {
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
