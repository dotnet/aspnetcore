// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    /// <summary>
    /// Manages the generation of the date header value.
    /// </summary>
    internal class DateHeaderValueManager : IHeartbeatHandler
    {
        // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
        private static ReadOnlySpan<byte> DatePreambleBytes => new byte[8] { (byte)'\r', (byte)'\n', (byte)'D', (byte)'a', (byte)'t', (byte)'e', (byte)':', (byte)' ' };

        private DateHeaderValues _dateValues;

        /// <summary>
        /// Returns a value representing the current server date/time for use in the HTTP "Date" response header
        /// in accordance with http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.18
        /// </summary>
        /// <returns>The value in string and byte[] format.</returns>
        public DateHeaderValues GetDateHeaderValues() => _dateValues;

        // Called by the Timer (background) thread
        public void OnHeartbeat(DateTimeOffset now)
        {
            SetDateValues(now);
        }

        /// <summary>
        /// Sets date values from a provided ticks value
        /// </summary>
        /// <param name="value">A DateTimeOffset value</param>
        private void SetDateValues(DateTimeOffset value)
        {
            var dateValue = HeaderUtilities.FormatDate(value);
            var dateBytes = new byte[DatePreambleBytes.Length + dateValue.Length];
            DatePreambleBytes.CopyTo(dateBytes);
            Encoding.ASCII.GetBytes(dateValue, dateBytes.AsSpan(DatePreambleBytes.Length));

            var dateValues = new DateHeaderValues
            {
                Bytes = dateBytes,
                String = dateValue
            };
            Volatile.Write(ref _dateValues, dateValues);
        }

        public class DateHeaderValues
        {
            public byte[] Bytes;
            public string String;
        }
    }
}
