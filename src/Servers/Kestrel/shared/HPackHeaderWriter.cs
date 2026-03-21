// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.HPack;

#if !(IS_TESTS || IS_BENCHMARKS)
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;
#endif

internal enum HeaderWriteResult : byte
{
    // Not all headers written.
    MoreHeaders = 0,

    // All headers written.
    Done = 1,

    // Oversized header for the given buffer.
    BufferTooSmall = 2,
}

// This file is used by Kestrel to write response headers and tests to write request headers.
// To avoid adding test code to Kestrel this file is shared. Test specifc code is excluded from Kestrel by ifdefs.
internal static class HPackHeaderWriter
{
    /// <summary>
    /// Begin encoding headers in the first HEADERS frame.
    /// </summary>
    public static HeaderWriteResult BeginEncodeHeaders(int statusCode, DynamicHPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
    {
        length = 0;

        if (!hpackEncoder.EnsureDynamicTableSizeUpdate(buffer, out var sizeUpdateLength))
        {
            throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
        }
        length += sizeUpdateLength;

        if (!EncodeStatusHeader(statusCode, hpackEncoder, buffer.Slice(length), out var statusCodeLength))
        {
            throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
        }
        length += statusCodeLength;

        if (!headersEnumerator.MoveNext())
        {
            return HeaderWriteResult.Done;
        }

        // Since we've already encoded the status, we know we didn't start with an empty buffer.  We don't need to increase it immediately because
        // There is a small chance that the header will encode if there is no other content in the next HEADERS frame.
        var done = EncodeHeadersCore(hpackEncoder, headersEnumerator, buffer.Slice(length), canRequestLargerBuffer: false, out var headersLength);
        length += headersLength;
        return done;
    }

    /// <summary>
    /// Begin encoding headers in the first HEADERS frame.
    /// </summary>
    public static HeaderWriteResult BeginEncodeHeaders(DynamicHPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length) =>
        BeginEncodeHeaders(hpackEncoder, headersEnumerator, buffer, iterateBeforeFirstElement: true, out length);

    /// <summary>
    /// Begin encoding headers in the first HEADERS frame without stepping the iterator.
    /// </summary>
    public static HeaderWriteResult RetryBeginEncodeHeaders(DynamicHPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length) =>
        BeginEncodeHeaders(hpackEncoder, headersEnumerator, buffer, iterateBeforeFirstElement: false, out length);

    /// <summary>
    /// Begin encoding headers in the first HEADERS frame.
    /// </summary>
    private static HeaderWriteResult BeginEncodeHeaders(DynamicHPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, bool iterateBeforeFirstElement, out int length)
    {
        length = 0;

        if (!hpackEncoder.EnsureDynamicTableSizeUpdate(buffer, out var sizeUpdateLength))
        {
            throw new HPackEncodingException(SR.net_http_hpack_encode_failure);
        }
        length += sizeUpdateLength;

        if (iterateBeforeFirstElement && !headersEnumerator.MoveNext())
        {
            return HeaderWriteResult.Done;
        }

        var done = EncodeHeadersCore(hpackEncoder, headersEnumerator, buffer.Slice(length), canRequestLargerBuffer: true, out var headersLength);
        length += headersLength;
        return done;
    }

    /// <summary>
    /// Continue encoding headers in the next HEADERS frame. The enumerator should already have a current value.
    /// </summary>
    public static HeaderWriteResult ContinueEncodeHeaders(DynamicHPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, out int length)
    {
        return EncodeHeadersCore(hpackEncoder, headersEnumerator, buffer, canRequestLargerBuffer: true, out length);
    }

    private static bool EncodeStatusHeader(int statusCode, DynamicHPackEncoder hpackEncoder, Span<byte> buffer, out int length)
    {
        if (H2StaticTable.TryGetStatusIndex(statusCode, out var index))
        {
            // Status codes which exist in the HTTP/2 StaticTable.
            return HPackEncoder.EncodeIndexedHeaderField(index, buffer, out length);
        }
        else
        {
            const string name = ":status";
            var value = StatusCodes.ToStatusString(statusCode);
            return hpackEncoder.EncodeHeader(buffer, H2StaticTable.Status200, HeaderEncodingHint.Index, name, value, valueEncoding: null, out length);
        }
    }

    private static HeaderWriteResult EncodeHeadersCore(DynamicHPackEncoder hpackEncoder, Http2HeadersEnumerator headersEnumerator, Span<byte> buffer, bool canRequestLargerBuffer, out int length)
    {
        var currentLength = 0;
        do
        {
            var staticTableId = headersEnumerator.HPackStaticTableId;
            var name = headersEnumerator.Current.Key;
            var value = headersEnumerator.Current.Value;
            var valueEncoding =
                ReferenceEquals(headersEnumerator.EncodingSelector, KestrelServerOptions.DefaultHeaderEncodingSelector)
                ? null : headersEnumerator.EncodingSelector(name);

            var hint = ResolveHeaderEncodingHint(staticTableId, name);

            if (!hpackEncoder.EncodeHeader(
                buffer.Slice(currentLength),
                staticTableId,
                hint,
                name,
                value,
                valueEncoding,
                out var headerLength))
            {
                // If the header wasn't written, and no headers have been written, then the header is too large.
                // Request for a larger buffer to write large header.
                if (currentLength == 0 && canRequestLargerBuffer)
                {
                    length = 0;
                    return HeaderWriteResult.BufferTooSmall;
                }
                length = currentLength;
                return HeaderWriteResult.MoreHeaders;
            }

            currentLength += headerLength;
        }
        while (headersEnumerator.MoveNext());
        length = currentLength;
        return HeaderWriteResult.Done;
    }

    private static HeaderEncodingHint ResolveHeaderEncodingHint(int staticTableId, string name)
    {
        HeaderEncodingHint hint;
        if (IsSensitive(staticTableId, name))
        {
            hint = HeaderEncodingHint.NeverIndex;
        }
        else if (IsNotDynamicallyIndexed(staticTableId))
        {
            hint = HeaderEncodingHint.IgnoreIndex;
        }
        else
        {
            hint = HeaderEncodingHint.Index;
        }

        return hint;
    }

    private static bool IsSensitive(int staticTableIndex, string name)
    {
        // Set-Cookie could contain sensitive data.
        switch (staticTableIndex)
        {
            case H2StaticTable.SetCookie:
            case H2StaticTable.ContentDisposition:
                return true;
            case -1:
                // Content-Disposition currently isn't a known header so a
                // static index probably won't be specified.
                return string.Equals(name, "Content-Disposition", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsNotDynamicallyIndexed(int staticTableIndex)
    {
        // Content-Length is added to static content. Content length is different for each
        // file, and is unlikely to be reused because of browser caching.
        return staticTableIndex == H2StaticTable.ContentLength;
    }
}
