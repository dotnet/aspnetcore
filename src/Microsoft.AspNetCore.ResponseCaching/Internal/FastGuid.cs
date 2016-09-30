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
        internal long IdValue { get; private set; }

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

        private static unsafe string GenerateGuidString(FastGuid guid)
        {
            // stackalloc to allocate array on stack rather than heap
            char* charBuffer = stackalloc char[13];

            // ID
            charBuffer[0] = _encode32Chars[(int)(guid.IdValue >> 60) & 31];
            charBuffer[1] = _encode32Chars[(int)(guid.IdValue >> 55) & 31];
            charBuffer[2] = _encode32Chars[(int)(guid.IdValue >> 50) & 31];
            charBuffer[3] = _encode32Chars[(int)(guid.IdValue >> 45) & 31];
            charBuffer[4] = _encode32Chars[(int)(guid.IdValue >> 40) & 31];
            charBuffer[5] = _encode32Chars[(int)(guid.IdValue >> 35) & 31];
            charBuffer[6] = _encode32Chars[(int)(guid.IdValue >> 30) & 31];
            charBuffer[7] = _encode32Chars[(int)(guid.IdValue >> 25) & 31];
            charBuffer[8] = _encode32Chars[(int)(guid.IdValue >> 20) & 31];
            charBuffer[9] = _encode32Chars[(int)(guid.IdValue >> 15) & 31];
            charBuffer[10] = _encode32Chars[(int)(guid.IdValue >> 10) & 31];
            charBuffer[11] = _encode32Chars[(int)(guid.IdValue >> 5) & 31];
            charBuffer[12] = _encode32Chars[(int)guid.IdValue & 31];

            // string ctor overload that takes char*
            return new string(charBuffer, 0, 13);
        }
    }
}
