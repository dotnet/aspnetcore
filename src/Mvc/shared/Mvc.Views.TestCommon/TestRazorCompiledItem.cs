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
        public TestRazorCompiledItem(Type type, string kind, string identifier, object[] metadata)
        {
            Type = type;
            Kind = kind;
            Identifier = identifier;
            Metadata = metadata;
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