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
    public class DateHeaderValueManager : IHeartbeatHandler
    {
        private static readonly byte[] _datePreambleBytes = Encoding.ASCII.GetBytes("\r\nDate: ");

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
            var dateBytes = new byte[_datePreambleBytes.Length + dateValue.Length];
            Buffer.BlockCopy(_datePreambleBytes, 0, dateBytes, 0, _datePreambleBytes.Length);
            Encoding.ASCII.GetBytes(dateValue, 0, dateValue.Length, dateBytes, _datePreambleBytes.Length);

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
