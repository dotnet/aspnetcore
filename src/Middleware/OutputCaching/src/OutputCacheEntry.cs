// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCacheEntry : IDisposable
{
    public OutputCacheEntry(DateTimeOffset created, int statusCode)
    {
        Created = created;
        StatusCode = statusCode;
    }

    private bool _recycleBuffers; // does this instance own the memory behind the segments?

    public StringValues FindHeader(string key)
    {
        TryFindHeader(key, out var value);
        return value;
    }

    public bool TryFindHeader(string key, out StringValues values)
    {
        foreach (var header in Headers.Span)
        {
            if (string.Equals(key, header.Name, StringComparison.OrdinalIgnoreCase))
            {
                values = header.Value;
                return true;
            }
        }
        values = StringValues.Empty;
        return false;
    }

    /// <summary>
    /// Gets the created date and time of the cache entry.
    /// </summary>
    public DateTimeOffset Created { get; }

    /// <summary>
    /// Gets the status code of the cache entry.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the headers of the cache entry.
    /// </summary>
    public ReadOnlyMemory<(string Name, StringValues Value)> Headers { get; private set; }

    // this is intentionally not an internal setter to make it clear that this should not be
    // used from most scenarios; this should consider buffer reuse - you *probably* want CopyFrom
    internal void SetHeaders(ReadOnlyMemory<(string Name, StringValues Value)> value) => Headers = value;

    /// <summary>
    /// Gets the body of the cache entry.
    /// </summary>
    public ReadOnlySequence<byte> Body { get; private set; }

    // this is intentionally not an internal setter to make it clear that this should not be
    // used from most scenarios; this should consider buffer reuse - you *probably* want CopyFrom
    internal void SetBody(ReadOnlySequence<byte> value, bool recycleBuffers)
    {
        Body = value;
        _recycleBuffers = recycleBuffers;
    }

    public void Dispose()
    {
        var headers = Headers;
        var body = Body;
        Headers = default;
        Body = default;
        Recycle(headers);
        RecyclableReadOnlySequenceSegment.RecycleChain(body, _recycleBuffers);
        // ^^ note that this only recycles the chain, not the actual buffers
    }

    private static void Recycle<T>(ReadOnlyMemory<T> value)
    {
        if (MemoryMarshal.TryGetArray<T>(value, out var segment) && segment.Array is { Length: > 0 })
        {
            ArrayPool<T>.Shared.Return(segment.Array);
        }
    }

    internal OutputCacheEntry CreateBodyFrom(IList<byte[]> segments) // mainly used from tests
    {
        // only expected in create path; don't reset/recycle existing
        Body = RecyclableReadOnlySequenceSegment.CreateSequence(segments);
        return this;
    }

    internal OutputCacheEntry CopyHeadersFrom(IHeaderDictionary headers)
    {
        // only expected in create path; don't reset/recycle existing
        if (headers is not null)
        {
            var count = headers.Count;
            var index = 0;
            if (count != 0)
            {
                var arr = ArrayPool<(string, StringValues)>.Shared.Rent(count);
                foreach (var header in headers)
                {
                    if (OutputCacheEntryFormatter.ShouldStoreHeader(header.Key))
                    {
                        arr[index++] = (header.Key, header.Value);
                    }
                }
                if (index == 0) // only ignored headers
                {
                    ArrayPool<(string, StringValues)>.Shared.Return(arr);
                }
                else
                {
                    Headers = new(arr, 0, index);
                }
            }
        }
        return this;
    }

    public void CopyHeadersTo(IHeaderDictionary headers)
    {
        if (!TryFindHeader(HeaderNames.TransferEncoding, out _))
        {
            headers.ContentLength = Body.Length;
        }
        foreach (var header in Headers.Span)
        {
            headers[header.Name] = header.Value;
        }
    }

    public ValueTask CopyToAsync(PipeWriter destination, CancellationToken cancellationToken)
        => RecyclableReadOnlySequenceSegment.CopyToAsync(Body, destination, cancellationToken);
}
