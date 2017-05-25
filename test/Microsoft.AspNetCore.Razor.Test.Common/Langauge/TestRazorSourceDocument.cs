// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestRazorSourceDocument
    {
        public static RazorSourceDocument CreateResource(string path, Type type, Encoding encoding = null, bool normalizeNewLines = false)
        {
            return CreateResource(path, type.GetTypeInfo().Assembly, encoding, normalizeNewLines);
        }

        public static RazorSourceDocument CreateResource(string path, Assembly assembly, Encoding encoding = null, bool normalizeNewLines = false)
        {
            var file = TestFile.Create(path, assembly);

            using (var input = file.OpenRead())
            using (var reader = new StreamReader(input))
            {
                var content = reader.ReadToEnd();
                if (normalizeNewLines)
                {
                    content = NormalizeNewLines(content);
                }

                return new StringSourceDocument(content, encoding ?? Encoding.UTF8, path);
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

            return new StringSourceDocument(content, encoding ?? Encoding.UTF8, fileName);
        }

        private static string NormalizeNewLines(string content)
        {
            return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
        }
    }
}
