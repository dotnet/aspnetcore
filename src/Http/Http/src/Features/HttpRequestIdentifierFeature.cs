// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="IHttpRequestIdentifierFeature"/>.
/// </summary>
public class HttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
{
    // Base32 encoding - in ascii sort order for easy text based sorting
    private static readonly char[] s_encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();
    // Seed the _requestId for this application instance with
    // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
    // for a roughly increasing _requestId over restarts
    private static long _requestId = DateTime.UtcNow.Ticks;

    private string? _id;

    /// <inheritdoc />
    public string TraceIdentifier
    {
        get
        {
            // Don't incur the cost of generating the request ID until it's asked for
            if (_id == null)
            {
                _id = GenerateRequestId(Interlocked.Increment(ref _requestId));
            }
            return _id;
        }
        set
        {
            _id = value;
        }
    }

    private static string GenerateRequestId(long id)
    {
        return string.Create(13, id, (buffer, value) =>
        {
            var encode32Chars = s_encode32Chars;

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
