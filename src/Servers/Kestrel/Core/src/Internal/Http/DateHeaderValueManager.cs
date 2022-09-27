// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// Manages the generation of the date header value.
/// </summary>
internal sealed class DateHeaderValueManager : IHeartbeatHandler
{
    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> DatePreambleBytes => "\r\nDate: "u8;

    private DateHeaderValues? _dateValues;

    /// <summary>
    /// Returns a value representing the current server date/time for use in the HTTP "Date" response header
    /// in accordance with http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.18
    /// </summary>
    /// <returns>The value in string and byte[] format.</returns>
    public DateHeaderValues GetDateHeaderValues() => _dateValues!;

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

        var dateValues = new DateHeaderValues(dateBytes, dateValue);
        Volatile.Write(ref _dateValues, dateValues);
    }

    public sealed class DateHeaderValues
    {
        public readonly byte[] Bytes;
        public readonly string String;

        public DateHeaderValues(byte[] bytes, string s)
        {
            Bytes = bytes;
            String = s;
        }
    }
}
