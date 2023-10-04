// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
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
        _usageCounter = 1; // creator has a hook automatically
    }

    private bool _recycleBuffers; // does this instance own the memory behind the segments?
    private int _usageCounter;

    [Conditional("DEBUG")]
    private void DebugAssertNotRecycled()
        => Debug.Assert(Volatile.Read(ref _usageCounter) > 0, nameof(OutputCacheEntry) + " recycled");

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

    private ReadOnlyMemory<(string Name, StringValues Value)> _headers;

    /// <summary>
    /// Gets the headers of the cache entry.
    /// </summary>
    public ReadOnlyMemory<(string Name, StringValues Value)> Headers
    {
        get
        {
            DebugAssertNotRecycled();
            return _headers;
        }
    }

    // this is intentionally not an internal setter to make it clear that this should not be
    // used from most scenarios; this should consider buffer reuse - you *probably* want CopyFrom
    internal void SetHeaders(ReadOnlyMemory<(string Name, StringValues Value)> value)
    {
        DebugAssertNotRecycled();
        _headers = value;
    }

    private ReadOnlySequence<byte> _body;
    /// <summary>
    /// Gets the body of the cache entry.
    /// </summary>
    public ReadOnlySequence<byte> Body
    {
        get
        {
            DebugAssertNotRecycled();
            return _body;
        }
    }

    // this is intentionally not an internal setter to make it clear that this should not be
    // used from most scenarios; this should consider buffer reuse - you *probably* want CopyFrom
    internal void SetBody(ReadOnlySequence<byte> value, bool recycleBuffers)
    {
        DebugAssertNotRecycled();
        _body = value;
        _recycleBuffers = recycleBuffers;
    }

    private void Recycle()
    {
        var headers = _headers;
        var body = _body;
        _headers = default;
        _body = default;
        Recycle(headers);
        RecyclableReadOnlySequenceSegment.RecycleChain(body, _recycleBuffers);
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
        DebugAssertNotRecycled();
        _body = RecyclableReadOnlySequenceSegment.CreateSequence(segments);
        return this;
    }

    internal OutputCacheEntry CopyHeadersFrom(IHeaderDictionary headers)
    {
        DebugAssertNotRecycled();
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
                    _headers = default;
                }
                else
                {
                    _headers = new(arr, 0, index);
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

    public void Dispose() => Release();

    /// <summary>
    /// Increment the usage counter *if we haven't already recycled* 
    /// </summary>
    /// <returns>true if the counter was successfully incremented, and this value can be safely used until <see cref="Release"/> is called</returns>
    public bool TryPreserve()
    {
        int oldCount, newCount;
        do // CEX retry loop
        {
            oldCount = Volatile.Read(ref _usageCounter);
            newCount = oldCount + 1;
            if (oldCount <= 0 || newCount <= 0)
            {
                // either already released, or we overflowed
                return false;
            }
        }
        while (Interlocked.CompareExchange(ref _usageCounter, newCount, oldCount) != oldCount);
        return true;
    }

    /// <summary>
    /// Decrement the usage counter by one; if we hit zero, recycle
    /// </summary>
    /// <returns>True if this operation caused it to become recycled (monitoring/logging only)</returns>
    public bool Release()
    {
        int oldCount;
        do // CEX retry loop
        {
            oldCount = Volatile.Read(ref _usageCounter);
            if (oldCount <= 0)
            {
                // already released; nothing to do
                // (note we can't underflow when subtracting 1 from a +ve number)
                return false;
            }
        }
        while (Interlocked.CompareExchange(ref _usageCounter, oldCount - 1, oldCount) != oldCount);
        if (oldCount == 1)
        {
            // then this was the final hook
            Recycle();
            return true;
        }
        return false;
    }

    internal async Task DelayedReleaseAsync(int millisecondsDelay)
    {
        try
        {
            await Task.Delay(millisecondsDelay);
            Release();
        }
        catch (Exception ex)
        {
            // we expect this to be in the background; we do *not* want
            // to have an orphan exception (although we also never
            // expect this to fail!)
            Debug.WriteLine(ex.Message);
        }
    }
}
