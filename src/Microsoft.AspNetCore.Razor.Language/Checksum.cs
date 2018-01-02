// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal static class Checksum
    {
        public static string BytesToString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
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
