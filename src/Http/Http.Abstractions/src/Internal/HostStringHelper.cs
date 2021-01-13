// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    internal class HostStringHelper
    {
        // Allowed Characters:
        // A-Z, a-z, 0-9, ., 
        // -, %, [, ], : 
        // Above for IPV6
        private static bool[] SafeHostStringChars = {
            false, false, false, false, false, false, false, false,     // 0x00 - 0x07
            false, false, false, false, false, false, false, false,     // 0x08 - 0x0F
            false, false, false, false, false, false, false, false,     // 0x10 - 0x17
            false, false, false, false, false, false, false, false,     // 0x18 - 0x1F
            false, false, false, false, false, true,  false, false,     // 0x20 - 0x27
            false, false, false, false, false, true,  true,  false,     // 0x28 - 0x2F
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x30 - 0x37
            true,  true,  true,  false, false, false, false, false,     // 0x38 - 0x3F
            false, true,  true,  true,  true,  true,  true,  true,      // 0x40 - 0x47
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x48 - 0x4F
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x50 - 0x57
            true,  true,  true,  true,  false, true,  false, false,     // 0x58 - 0x5F
            false, true,  true,  true,  true,  true,  true,  true,      // 0x60 - 0x67
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x68 - 0x6F
            true,  true,  true,  true,  true,  true,  true,  true,      // 0x70 - 0x77
            true,  true,  true,  false, false, false, false, false,     // 0x78 - 0x7F
        };

        public static bool IsSafeHostStringChar(char c)
        {
            return c < SafeHostStringChars.Length && SafeHostStringChars[c];
        }
    }
}
