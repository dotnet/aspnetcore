// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class TestRazorSourceDocument : DefaultRazorSourceDocument
    {
        private TestRazorSourceDocument(string content, Encoding encoding, string filename)
            : base(content, encoding, filename)
        {
        }

        public static RazorSourceDocument CreateResource(string path, Encoding encoding = null, bool normalizeNewLines = false)
        {
            var file = TestFile.Create(path);

            using (var input = file.OpenRead())
            using (var reader = new StreamReader(input))
            {
                var content = reader.ReadToEnd();
                if (normalizeNewLines)
                {
                    content = NormalizeNewLines(content);
                }

                return new TestRazorSourceDocument(content, encoding ?? Encoding.UTF8, path);
            }
        }

        public static MemoryStream CreateStreamContent(string content = "Hello, World!", Encoding encoding = null, bool normalizeNewLines = false)
        {
            var stream = new MemoryStream();
            encoding = encoding ?? Encoding.UTF8;
            using (var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true))
            {
                if (normalizeNewLines)
                {
                    content = NormalizeNewLines(content);
                }

                writer.Write(content);
            }

            stream.Seek(0L, SeekOrigin.Begin);

            return stream;
        }

        public static RazorSourceDocument Create(string content = "Hello, world!", Encoding encoding = null, bool normalizeNewLines = false, string fileName = "test.cshtml")
        {
            if (normalizeNewLines)
            {
                content = NormalizeNewLines(content);
            }

            return new TestRazorSourceDocument(content, encoding ?? Encoding.UTF8, fileName);
        }

        private static string NormalizeNewLines(string content)
        {
            return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
        }
    }
}
