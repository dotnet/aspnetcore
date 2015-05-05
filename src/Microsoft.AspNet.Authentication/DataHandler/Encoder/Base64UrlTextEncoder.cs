// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authentication.DataHandler.Encoder
{
    public class Base64UrlTextEncoder : ITextEncoder
    {
        public string Encode([NotNull] byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public byte[] Decode([NotNull] string text)
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
