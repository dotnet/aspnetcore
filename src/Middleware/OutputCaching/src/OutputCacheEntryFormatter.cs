// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;
/// <summary>
/// Formats <see cref="OutputCacheEntry"/> instance to match structures supported by the <see cref="IOutputCacheStore"/> implementations.
/// </summary>
internal static class OutputCacheEntryFormatter
{
    private enum SerializationRevision
    {
        V1_Original = 1,
        V2_OriginalWithCommonHeaders = 2,
    }

    public static async ValueTask<OutputCacheEntry?> GetAsync(string key, IOutputCacheStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        var content = await store.GetAsync(key, cancellationToken);

        if (content is null)
        {
            return null;
        }

        return Deserialize(content);
    }

    public static async ValueTask StoreAsync(string key, OutputCacheEntry value, HashSet<string>? tags, TimeSpan duration, IOutputCacheStore store, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);

        var buffer = new RecyclableArrayBufferWriter<byte>();
        Serialize(buffer, value);

        try
        {
            if (store is IOutputCacheBufferStore bufferStore)
            {
                await bufferStore.SetAsync(key, new(buffer.GetCommittedMemory()), CopyToLeasedMemory(tags, out var lease), duration, cancellationToken);
                if (lease is not null)
                {
                    ArrayPool<string>.Shared.Return(lease);
                }
            }
            else
            {
                // legacy API/in-proc: create an isolated right-sized byte[] for the payload
                string[] tagsArr = tags is { Count: > 0 } ? tags.ToArray() : Array.Empty<string>();
                await store.SetAsync(key, buffer.ToArray(), tagsArr, duration, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // don't report as failure
        }
        catch (Exception ex)
        {
            logger.UnableToWriteToOutputCache(ex);
        }
        buffer.Dispose(); // this is intentionally not using "using"; only recycle on success, to avoid async code accessing shared buffers (esp. in cancellation)

        static ReadOnlyMemory<string> CopyToLeasedMemory(HashSet<string>? tags, out string[]? lease)
        {
            if (tags is null || tags.Count == 0)
            {
                lease = null;
                return default;
            }
            int index = 0;
            lease = ArrayPool<string>.Shared.Rent(tags.Count);
            foreach (var tag in tags)
            {
                lease[index++] = tag;
            }
            return new ReadOnlyMemory<string>(lease, 0, index);
        }
    }

    // Format:
    // Serialization revision:
    //   7-bit encoded int
    // Creation date:
    //   Ticks: 7-bit encoded long
    //   Offset.TotalMinutes: 7-bit encoded long
    // Status code:
    //   7-bit encoded int
    // Headers:
    //   Headers count: 7-bit encoded int
    //   For each header:
    //     key name byte length: 7-bit encoded int
    //     UTF-8 encoded key name byte[]
    //     Values count: 7-bit encoded int
    //     For each header value:
    //       data byte length: 7-bit encoded int
    //       UTF-8 encoded byte[]
    // Body:
    //   Segments count: 7-bit encoded int
    //   For each segment:
    //     data byte length: 7-bit encoded int
    //     data byte[]
    // Tags:
    //   Tags count: 7-bit encoded int
    //   For each tag:
    //     data byte length: 7-bit encoded int
    //     UTF-8 encoded byte[]

    private static void Serialize(IBufferWriter<byte> output, OutputCacheEntry entry)
    {
        var writer = new FormatterBinaryWriter(output);

        // Serialization revision:
        //   7-bit encoded int
        writer.Write7BitEncodedInt((int)SerializationRevision.V2_OriginalWithCommonHeaders);

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        writer.Write7BitEncodedInt64(entry.Created.Ticks);
        writer.Write7BitEncodedInt64((long)entry.Created.Offset.TotalMinutes);

        // Status code:
        //   7-bit encoded int
        writer.Write7BitEncodedInt(entry.StatusCode);

        // Headers:
        //   Headers count: 7-bit encoded int

        writer.Write7BitEncodedInt(entry.Headers.Length);

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]

        foreach (var header in entry.Headers.Span)
        {
            WriteCommonHeader(ref writer, header.Name);

            //     Values count: 7-bit encoded int
            var count = header.Value.Count;
            writer.Write7BitEncodedInt(count);

            //     For each header value:
            //       data byte length: 7-bit encoded int
            //       UTF-8 encoded byte[]
            for (var i = 0; i < count; i++)
            {
                WriteCommonHeader(ref writer, header.Value[i]);
            }
        }

        // Body:
        //   Bytes count: 7-bit encoded int
        //     data byte[]

        var body = entry.Body;
        if (body.IsEmpty)
        {
            writer.Write((byte)0);
        }
        else if (body.IsSingleSegment)
        {
            var span = body.FirstSpan;
            writer.Write7BitEncodedInt(span.Length);
            writer.WriteRaw(span);
        }
        else
        {
            writer.Write7BitEncodedInt(checked((int)body.Length));
            foreach (var segment in body)
            {
                writer.WriteRaw(segment.Span);
            }
        }

        writer.Flush();
    }

    static void WriteCommonHeader(ref FormatterBinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write((byte)0);
        }
        else
        {
            if (CommonHeadersLookup.TryGetValue(value, out int known))
            {
                writer.Write7BitEncodedInt((known << 1) | 1);
            }
            else
            {
                // use the length-prefixed UTF8 write in FormatterBinaryWriter,
                // but with a left-shift applied
                writer.Write(value, lengthShift: 1);
            }
        }
    }

    private static bool CanParseRevision(SerializationRevision revision, out bool useCommonHeaders)
    {
        switch (revision)
        {
            case SerializationRevision.V1_Original: // we don't actively expect this much, since only in-proc back-end was shipped
                useCommonHeaders = false;
                return true;
            case SerializationRevision.V2_OriginalWithCommonHeaders:
                useCommonHeaders = true;
                return true;
            default:
                // In future versions, also support the previous revision format.
                useCommonHeaders = default;
                return false;
        }
    }

    internal static OutputCacheEntry? Deserialize(ReadOnlyMemory<byte> content)
    {
        var reader = new FormatterBinaryReader(content);

        // Serialization revision:
        //   7-bit encoded int

        var revision = (SerializationRevision)reader.Read7BitEncodedInt();
        if (!CanParseRevision(revision, out var useCommonHeaders))
        {
            return null;
        }

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        var ticks = reader.Read7BitEncodedInt64();
        var offsetMinutes = reader.Read7BitEncodedInt64();

        var created = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));

        // Status code:
        //   7-bit encoded int

        var statusCode = reader.Read7BitEncodedInt();

        var result = new OutputCacheEntry(created, statusCode);

        // Headers:
        //   Headers count: 7-bit encoded int

        var headersCount = reader.Read7BitEncodedInt();

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]
        //     Values count: 7-bit encoded int
        if (headersCount > 0)
        {
            var headerArr = ArrayPool<(string Name, StringValues Values)>.Shared.Rent(headersCount);

            for (var i = 0; i < headersCount; i++)
            {
                var key = useCommonHeaders ? ReadCommonHeader(ref reader) : reader.ReadString();
                StringValues value;
                var valuesCount = reader.Read7BitEncodedInt();
                //     For each header value:
                //       data byte length: 7-bit encoded int
                //       UTF-8 encoded byte[]
                switch (valuesCount)
                {
                    case < 0:
                        throw new InvalidOperationException();
                    case 0:
                        value = StringValues.Empty;
                        break;
                    case 1:
                        value = new(useCommonHeaders ? ReadCommonHeader(ref reader) : reader.ReadString());
                        break;
                    default:
                        var values = new string[valuesCount];

                        for (var j = 0; j < valuesCount; j++)
                        {
                            values[j] = useCommonHeaders ? ReadCommonHeader(ref reader) : reader.ReadString();
                        }
                        value = new(values);
                        break;
                }
                headerArr[i] = (key, value);
            }
            result.SetHeaders(new ReadOnlyMemory<(string Name, StringValues Values)>(headerArr, 0, headersCount));
        }

        if (revision == SerializationRevision.V1_Original)
        {
            // Body:
            //   Segments count: 7-bit encoded int

            var segmentsCount = reader.Read7BitEncodedInt();

            //   For each segment:
            //     data byte length: 7-bit encoded int
            //     data byte[]

            switch (segmentsCount)
            {
                case 0:
                    // nothing to do
                    break;
                case 1:
                    result.SetBody(new ReadOnlySequence<byte>(ReadSegment(ref reader)), recycleBuffers: false); // we're reusing the live payload buffers
                    break;
                case < 0:
                    throw new InvalidOperationException();
                default:
                    RecyclableReadOnlySequenceSegment first = RecyclableReadOnlySequenceSegment.Create(ReadSegment(ref reader), null), last = first;
                    for (int i = 1; i < segmentsCount; i++)
                    {
                        last = RecyclableReadOnlySequenceSegment.Create(ReadSegment(ref reader), last);
                    }
                    result.SetBody(new ReadOnlySequence<byte>(first, 0, last, last.Length), recycleBuffers: false);  // we're reusing the live payload buffers
                    break;
            }

            static ReadOnlyMemory<byte> ReadSegment(ref FormatterBinaryReader reader)
            {
                var segmentLength = reader.Read7BitEncodedInt();
                return reader.ReadBytesMemory(segmentLength);
            }

            // we can just stop reading, but: here's how we'd skip tags if we had to
            // (actually validate them in debug to prove reader)
#if DEBUG
            if (revision == SerializationRevision.V1_Original)
            {
                // Tags:
                //   Tags count: 7-bit encoded int

                var tagsCount = reader.Read7BitEncodedInt();
                if (tagsCount > 0)
                {
                    //   For each tag:
                    //     data byte length: 7-bit encoded int
                    //     UTF-8 encoded byte[]
                    for (var i = 0; i < tagsCount; i++)
                    {
                        reader.SkipString();
                    }
                }
            }
#endif
        }
        else
        {
            // Body:
            //   Bytes count: 7-bit encoded int

            var payloadLength = checked((int)reader.Read7BitEncodedInt64());
            if (payloadLength != 0)
            {   // since the reader only supports linear memory currently, read the entire chunk as a single piece
                result.SetBody(new(reader.ReadBytesMemory(payloadLength)), recycleBuffers: false); // we're reusing the live payload buffers
            }
        }

        Debug.Assert(reader.IsEOF, "should have read entire payload");
        return result;
    }

    private static string ReadCommonHeader(ref FormatterBinaryReader reader)
    {
        int preamble = reader.Read7BitEncodedInt();
        // LSB means "using common header/value"
        if ((preamble & 1) == 1)
        {
            // non-LSB is the index of the common header
            return CommonHeaders[preamble >> 1];
        }
        else
        {
            // non-LSB is the string length
            return reader.ReadString(preamble >> 1);
        }
    }

    static readonly string[] CommonHeaders = new string[]
    {
        // DO NOT remove values, and do not re-order/insert - append only
        // NOTE: arbitrary common strings are fine - it doesn't all have to be headers
        HeaderNames.Accept,
        HeaderNames.AcceptCharset,
        HeaderNames.AcceptEncoding,
        HeaderNames.AcceptLanguage,
        HeaderNames.AcceptRanges,
        HeaderNames.AccessControlAllowCredentials,
        HeaderNames.AccessControlAllowHeaders,
        HeaderNames.AccessControlAllowMethods,
        HeaderNames.AccessControlAllowOrigin,
        HeaderNames.AccessControlExposeHeaders,
        HeaderNames.AccessControlMaxAge,
        HeaderNames.AccessControlRequestHeaders,
        HeaderNames.AccessControlRequestMethod,
        HeaderNames.Age,
        HeaderNames.Allow,
        HeaderNames.AltSvc,
        HeaderNames.Authorization,
        HeaderNames.Baggage,
        HeaderNames.CacheControl,
        HeaderNames.Connection,
        HeaderNames.ContentDisposition,
        HeaderNames.ContentEncoding,
        HeaderNames.ContentLanguage,
        HeaderNames.ContentLength,
        HeaderNames.ContentLocation,
        HeaderNames.ContentMD5,
        HeaderNames.ContentRange,
        HeaderNames.ContentSecurityPolicy,
        HeaderNames.ContentSecurityPolicyReportOnly,
        HeaderNames.ContentType,
        HeaderNames.CorrelationContext,
        HeaderNames.Cookie,
        HeaderNames.Date,
        HeaderNames.DNT,
        HeaderNames.ETag,
        HeaderNames.Expires,
        HeaderNames.Expect,
        HeaderNames.From,
        HeaderNames.Host,
        HeaderNames.KeepAlive,
        HeaderNames.IfMatch,
        HeaderNames.IfModifiedSince,
        HeaderNames.IfNoneMatch,
        HeaderNames.IfRange,
        HeaderNames.IfUnmodifiedSince,
        HeaderNames.LastModified,
        HeaderNames.Link,
        HeaderNames.Location,
        HeaderNames.MaxForwards,
        HeaderNames.Origin,
        HeaderNames.Pragma,
        HeaderNames.ProxyAuthenticate,
        HeaderNames.ProxyAuthorization,
        HeaderNames.ProxyConnection,
        HeaderNames.Range,
        HeaderNames.Referer,
        HeaderNames.RequestId,
        HeaderNames.RetryAfter,
        HeaderNames.Server,
        HeaderNames.StrictTransportSecurity,
        HeaderNames.TE,
        HeaderNames.Trailer,
        HeaderNames.TransferEncoding,
        HeaderNames.Translate,
        HeaderNames.TraceParent,
        HeaderNames.TraceState,
        HeaderNames.Vary,
        HeaderNames.Via,
        HeaderNames.Warning,
        HeaderNames.XContentTypeOptions,
        HeaderNames.XFrameOptions,
        HeaderNames.XPoweredBy,
        HeaderNames.XRequestedWith,
        HeaderNames.XUACompatible,
        HeaderNames.XXSSProtection,
        // additional MSFT headers
        "X-Rtag",
        "X-Vhost",

        // for Content-Type
        "text/html",
        "text/html; charset=utf-8",
        "text/html;charset=utf-8",
        "text/xml",
        "text/json",
        "application/x-binary",
        "image/svg+xml",
        "image/x-png",
        // for Accept-Encoding
        "gzip",
        "compress",
        "deflate",
        "br",
        "identity",
        "*",
        // for X-Frame-Options
        "SAMEORIGIN",
        "DENY",
        // for X-Content-Type
        "nosniff"

        // if you add new options here, you should rev the api version
    };

    private static readonly FrozenSet<string> IgnoredHeaders = FrozenSet.ToFrozenSet(new[] {
            HeaderNames.RequestId, HeaderNames.ContentLength, HeaderNames.Age
    }, StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, int> CommonHeadersLookup = BuildCommonHeadersLookup();

    static FrozenDictionary<string, int> BuildCommonHeadersLookup()
    {
        var arr = CommonHeaders;
        var pairs = new List<KeyValuePair<string, int>>(arr.Length);
        for (var i = 0; i < arr.Length; i++)
        {
            var header = arr[i];
            if (!string.IsNullOrWhiteSpace(header)) // omit null/empty values
            {
                pairs.Add(new(header, i));
            }
        }

        return FrozenDictionary.ToFrozenDictionary(pairs, StringComparer.OrdinalIgnoreCase);
    }

    internal static bool ShouldStoreHeader(string key) => !IgnoredHeaders.Contains(key);
}
