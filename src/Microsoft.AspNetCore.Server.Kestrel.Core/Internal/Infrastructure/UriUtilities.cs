// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public class UriUtilities
    {
        /// <summary>
        /// Returns true if character is valid in the 'authority' section of a URI.
        /// <see href="https://tools.ietf.org/html/rfc3986#section-3.2"/>
        /// </summary>
        /// <param name="ch">The character</param>
        /// <returns></returns>
        public static bool IsValidAuthorityCharacter(byte ch)
        {
            // Examples:
            // microsoft.com
            // hostname:8080
            // [::]:8080
            // [fe80::]
            // 127.0.0.1
            // user@host.com
            // user:password@host.com
            return
                (ch >= '0' && ch <= '9') ||
                (ch >= 'A' && ch <= 'Z') ||
                (ch >= 'a' && ch <= 'z') ||
                ch == ':' ||
                ch == '.' ||
                ch == '[' ||
                ch == ']' ||
                ch == '@';
        }
    }
}
