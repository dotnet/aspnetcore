// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication
{
    public static class TextEncodings
    {
        private static readonly ITextEncoder Base64Instance = new Base64TextEncoder();
        private static readonly ITextEncoder Base64UrlInstance = new Base64UrlTextEncoder();

        public static ITextEncoder Base64
        {
            get { return Base64Instance; }
        }

        public static ITextEncoder Base64Url
        {
            get { return Base64UrlInstance; }
        }
    }
}
