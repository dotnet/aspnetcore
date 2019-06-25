// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class FastGuid
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly char[] s_encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();
        // Global ID
        private static long NextId;

        // Instance components
        private string _idString;

        internal long IdValue { get; }

        internal string IdString
        {
            get
            {
                if (_idString == null)
                {
                    _idString = GenerateGuidString(this);
                }
                return _idString;
            }
        }

        // Static constructor to initialize global components
        static FastGuid()
        {
            var guidBytes = Guid.NewGuid().ToByteArray();

            // Use the first 4 bytes from the Guid to initialize global ID
            NextId =
                guidBytes[0] << 32 |
                guidBytes[1] << 40 |
                guidBytes[2] << 48 |
                guidBytes[3] << 56;
        }

        internal FastGuid(long id)
        {
            IdValue = id;
        }

        internal static FastGuid NewGuid()
        {
            return new FastGuid(Interlocked.Increment(ref NextId));
        }

        private static string GenerateGuidString(FastGuid guid)
        {
            return string.Create(13, guid.IdValue, (buffer, value) =>
            {
                char[] encode32Chars = s_encode32Chars;
                buffer[12] = encode32Chars[value & 31];
                buffer[11] = encode32Chars[(value >> 5) & 31];
                buffer[10] = encode32Chars[(value >> 10) & 31];
                buffer[9] = encode32Chars[(value >> 15) & 31];
                buffer[8] = encode32Chars[(value >> 20) & 31];
                buffer[7] = encode32Chars[(value >> 25) & 31];
                buffer[6] = encode32Chars[(value >> 30) & 31];
                buffer[5] = encode32Chars[(value >> 35) & 31];
                buffer[4] = encode32Chars[(value >> 40) & 31];
                buffer[3] = encode32Chars[(value >> 45) & 31];
                buffer[2] = encode32Chars[(value >> 50) & 31];
                buffer[1] = encode32Chars[(value >> 55) & 31];
                buffer[0] = encode32Chars[(value >> 60) & 31];
            });
        }
    }
}
