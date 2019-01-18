// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal class FastGuid
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
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
            return string.Create(13, guid.IdValue, (span, value) =>
            {
                span[12] = _encode32Chars[(int)value & 31];
                span[11] = _encode32Chars[(int)(value >> 5) & 31];
                span[10] = _encode32Chars[(int)(value >> 10) & 31];
                span[9] = _encode32Chars[(int)(value >> 15) & 31];
                span[8] = _encode32Chars[(int)(value >> 20) & 31];
                span[7] = _encode32Chars[(int)(value >> 25) & 31];
                span[6] = _encode32Chars[(int)(value >> 30) & 31];
                span[5] = _encode32Chars[(int)(value >> 35) & 31];
                span[4] = _encode32Chars[(int)(value >> 40) & 31];
                span[3] = _encode32Chars[(int)(value >> 45) & 31];
                span[2] = _encode32Chars[(int)(value >> 50) & 31];
                span[1] = _encode32Chars[(int)(value >> 55) & 31];
                span[0] = _encode32Chars[(int)(value >> 60) & 31];
            });
        }
    }
}
