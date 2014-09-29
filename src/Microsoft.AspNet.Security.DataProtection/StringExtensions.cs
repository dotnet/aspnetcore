// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal static class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetTotalByteLengthIncludingNullTerminator(this string input)
        {
            if (input == null)
            {
                // degenerate case
                return 0;
            }
            else
            {
                uint numChars = (uint)input.Length + 1U; // no overflow check necessary since Length is signed
                return checked(numChars * sizeof(char));
            }
        }
    }
}
