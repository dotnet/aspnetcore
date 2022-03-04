// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace PlatformBenchmarks
{
    /// <summary>
    /// Manages the generation of the date header value.
    /// </summary>
    internal static class DateHeader
    {
        const int prefixLength = 8; // "\r\nDate: ".Length
        const int dateTimeRLength = 29; // Wed, 14 Mar 2018 14:20:00 GMT
        const int suffixLength = 2; // crlf
        const int suffixIndex = dateTimeRLength + prefixLength;

        private static readonly Timer s_timer = new Timer((s) => {
            SetDateValues(DateTimeOffset.UtcNow);
        }, null, 1000, 1000);

        private static byte[] s_headerBytesMaster = new byte[prefixLength + dateTimeRLength + suffixLength];
        private static byte[] s_headerBytesScratch = new byte[prefixLength + dateTimeRLength + suffixLength];

        static DateHeader()
        {
            var utf8 = Encoding.ASCII.GetBytes("\r\nDate: ").AsSpan();
            utf8.CopyTo(s_headerBytesMaster);
            utf8.CopyTo(s_headerBytesScratch);
            s_headerBytesMaster[suffixIndex] = (byte)'\r';
            s_headerBytesMaster[suffixIndex + 1] = (byte)'\n';
            s_headerBytesScratch[suffixIndex] = (byte)'\r';
            s_headerBytesScratch[suffixIndex + 1] = (byte)'\n';
            SetDateValues(DateTimeOffset.UtcNow);
            SyncDateTimer();
        }

        public static void SyncDateTimer() => s_timer.Change(1000, 1000);

        public static ReadOnlySpan<byte> HeaderBytes => s_headerBytesMaster;

        private static void SetDateValues(DateTimeOffset value)
        {
            lock (s_headerBytesScratch)
            {
                if (!Utf8Formatter.TryFormat(value, s_headerBytesScratch.AsSpan(prefixLength), out int written, 'R'))
                {
                    throw new Exception("date time format failed");
                }
                Debug.Assert(written == dateTimeRLength);
                var temp = s_headerBytesMaster;
                s_headerBytesMaster = s_headerBytesScratch;
                s_headerBytesScratch = temp;
            }
        }
    }
}