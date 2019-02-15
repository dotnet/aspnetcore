// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Hosting
{
    public class TestRazorCompiledItem : RazorCompiledItem
    {
        public static RazorCompiledItem CreateForPage(string identifier, object[] metadata = null)
        {
            return CreateForPage(type: null, identifier, metadata);
        }

        public static RazorCompiledItem CreateForPage(Type type, string identifier, object[] metadata = null)
        {
            return new TestRazorCompiledItem(type, "mvc.1.0.razor-page", identifier, metadata);
        }

        public static RazorCompiledItem CreateForView(string identifier, object[] metadata = null)
        {
            return CreateForView(type: null, identifier, metadata);
        }

        public static RazorCompiledItem CreateForView(Type type, string identifier, object[] metadata = null)
        {
            return new TestRazorCompiledItem(type, "mvc.1.0.razor-page", identifier, metadata);
        }

        public TestRazorCompiledItem(Type type, string kind, string identifier, object[] metadata)
        {
            Type = type;
            Kind = kind;
            Identifier = identifier;
            Metadata = metadata ?? Array.Empty<object>();
        }

        public override string Identifier { get; }

        public override string Kind { get; }

        public override IReadOnlyList<object> Metadata { get; }

        public override Type Type { get; }

        public static string GetChecksum(string content)
        {
            byte[] bytes;
            using (var sha = SHA1.Create())
            {
                bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
            }

            var result = new StringBuilder(bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
            {
                // The x2 format means lowercase hex, where each byte is a 2-character string.
                result.Append(bytes[i].ToString("x2"));
            }

            return result.ToString();
        }
    }
}