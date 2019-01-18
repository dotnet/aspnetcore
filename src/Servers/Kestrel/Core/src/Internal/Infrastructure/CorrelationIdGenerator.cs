// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static class CorrelationIdGenerator
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        // Seed the _lastConnectionId for this application instance with
        // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
        // for a roughly increasing _lastId over restarts
        private static long _lastId = DateTime.UtcNow.Ticks;

        public static string GetNextId() => GenerateId(Interlocked.Increment(ref _lastId));

        private static string GenerateId(long id)
        {
            // The following routine is ~310% faster than calling long.ToString() on x64
            // and ~600% faster than calling long.ToString() on x86 in tight loops of 1 million+ iterations
            // See: https://github.com/aspnet/Hosting/pull/385

            return string.Create(13, id, (span, value) =>
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
