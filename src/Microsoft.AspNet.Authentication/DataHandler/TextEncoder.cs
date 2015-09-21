// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Authentication
{
    public static class Base64UrlTextEncoder
    {
        public static string Encode(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static byte[] Decode(string text)
        {
            return Convert.FromBase64String(Pad(text.Replace('-', '+').Replace('_', '/')));
        }

        private static string Pad(string text)
        {
            var padding = 3 - ((text.Length + 3) % 4);
            if (padding == 0)
            {
                return text;
            }
            return text + new string('=', padding);
        }

    }
}
