// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Microbenchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
public class EndToEndBenchmarks
{
    [Params(10, 1000, (64 * 1024) + 17, (256 * 1024) + 17)]
    public int PayloadLength { get; set; } = 1024; // default for simple runs

    private byte[] _payloadOversized = Array.Empty<byte>();
    private string Key = "";
    private IOutputCacheStore _store = null!;

    private static readonly OutputCacheOptions _options = new();
    private static readonly Action _noop = () => { };

    private static readonly HashSet<string> _tags = new();
    private static IHeaderDictionary _headers = null!;

    private ReadOnlyMemory<byte> Payload => new(_payloadOversized, 0, PayloadLength);

    [GlobalCleanup]
    public void Cleanup()
    {
        var arr = _payloadOversized;
        _payloadOversized = Array.Empty<byte>();
        if (arr.Length != 0)
        {
            ArrayPool<byte>.Shared.Return(arr);
        }
        _store = null!;
        _headers = null!;
    }

    [GlobalSetup]
    public async Task InitAsync()
    {
        Key = Guid.NewGuid().ToString();
        _store = new DummyStore(Key);
        _payloadOversized = ArrayPool<byte>.Shared.Rent(PayloadLength);
        Random.Shared.NextBytes(_payloadOversized);
        // some random headers from ms.com
        _headers = new HeaderDictionary
        {
            ContentLength = PayloadLength,
            ["X-Rtag"] = "AEM_PROD_Marketing",
            ["X-Vhost"] = "publish_microsoft_s",
        };
        _headers.ContentType = "text/html;charset=utf-8";
        _headers.Vary = "Accept-Encoding";
        _headers.XContentTypeOptions = "nosniff";
        _headers.XFrameOptions = "SAMEORIGIN";
        _headers.RequestId = Key;

        // store, fetch, validate (for each impl)
        await StreamSync();
        await ReadAsync(true);

        await StreamAsync();
        await ReadAsync(true);

        await WriterAsync();
        await ReadAsync(true);
    }

    static void WriteInRandomChunks(ReadOnlySpan<byte> value, Stream destination)
    {
        var rand = Random.Shared;
        while (!value.IsEmpty)
        {
            var bytes = Math.Min(rand.Next(4, 1024), value.Length);
            destination.Write(value.Slice(0, bytes));
            value = value.Slice(bytes);
        }
        destination.Flush();
    }

    static Task WriteInRandomChunks(ReadOnlyMemory<byte> source, PipeWriter destination, CancellationToken cancellationToken)
    {
        var value = source.Span;
        var rand = Random.Shared;
        while (!value.IsEmpty)
        {
            var bytes = Math.Min(rand.Next(4, 1024), value.Length);
            var span = destination.GetSpan(bytes);
            bytes = Math.Min(bytes, span.Length);
            value.Slice(0, bytes).CopyTo(span);
            destination.Advance(bytes);
            value = value.Slice(bytes);
        }
        return destination.FlushAsync(cancellationToken).AsTask();
    }

    static async Task WriteInRandomChunksAsync(ReadOnlyMemory<byte> value, Stream destination, CancellationToken cancellationToken)
    {
        var rand = Random.Shared;
        while (!value.IsEmpty)
        {
            var bytes = Math.Min(rand.Next(4, 1024), value.Length);
            await destination.WriteAsync(value.Slice(0, bytes), cancellationToken);
            value = value.Slice(bytes);
        }
        await destination.FlushAsync(cancellationToken);
    }

    [Benchmark(Description = "StreamSync"), BenchmarkCategory("Write")]
    public async Task StreamSync()
    {
        ReadOnlySequence<byte> body;
        using (var oc = new OutputCacheStream(Stream.Null, _options.MaximumBodySize, StreamUtilities.BodySegmentSize, _noop))
        {
            WriteInRandomChunks(Payload.Span, oc);
            body = oc.GetCachedResponseBody();
        }
        var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK)
            .CopyHeadersFrom(_headers);
        entry.SetBody(body, recycleBuffers: true);
        await OutputCacheEntryFormatter.StoreAsync(Key, entry, _tags, _options.DefaultExpirationTimeSpan, _store, NullLogger.Instance, CancellationToken.None);
        entry.Dispose();
    }

    [Benchmark(Description = "StreamAsync"), BenchmarkCategory("Write")]
    public async Task StreamAsync()
    {
        ReadOnlySequence<byte> body;
        using (var oc = new OutputCacheStream(Stream.Null, _options.MaximumBodySize, StreamUtilities.BodySegmentSize, _noop))
        {
            await WriteInRandomChunksAsync(Payload, oc, CancellationToken.None);
            body = oc.GetCachedResponseBody();
        }
        var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK)
            .CopyHeadersFrom(_headers);
        entry.SetBody(body, recycleBuffers: true);
        await OutputCacheEntryFormatter.StoreAsync(Key, entry, _tags, _options.DefaultExpirationTimeSpan, _store, NullLogger.Instance, CancellationToken.None);
        entry.Dispose();
    }

    [Benchmark(Description = "BodyWriter"), BenchmarkCategory("Write")]
    public async Task WriterAsync()
    {
        ReadOnlySequence<byte> body;
        using (var oc = new OutputCacheStream(Stream.Null, _options.MaximumBodySize, StreamUtilities.BodySegmentSize, _noop))
        {
            var pipe = PipeWriter.Create(oc, new StreamPipeWriterOptions(leaveOpen: true));
            await WriteInRandomChunks(Payload, pipe, CancellationToken.None);
            body = oc.GetCachedResponseBody();
        }
        var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK)
            .CopyHeadersFrom(_headers);
        entry.SetBody(body, recycleBuffers: true);
        await OutputCacheEntryFormatter.StoreAsync(Key, entry, _tags, _options.DefaultExpirationTimeSpan, _store, NullLogger.Instance, CancellationToken.None);
        entry.Dispose();
    }

    [Benchmark, BenchmarkCategory("Read")]
    public Task ReadAsync() => ReadAsync(false);

    private async Task ReadAsync(bool validate)
    {
        static void ThrowNotFound() => throw new KeyNotFoundException();

        var entry = await OutputCacheEntryFormatter.GetAsync(Key, _store, CancellationToken.None);
        if (validate)
        {
            Validate(entry!);
        }
        if (entry is null)
        {
            ThrowNotFound();
        }
        else
        {
            entry.Dispose();
        }
    }

    private void Validate(OutputCacheEntry value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var body = value.Body;
        if (body.Length != PayloadLength)
        {
            throw new InvalidOperationException("Invalid payload length");
        }

        if (body.IsSingleSegment)
        {
            if (!Payload.Span.SequenceEqual(body.FirstSpan))
            {
                throw new InvalidOperationException("Invalid payload");
            }
        }
        else
        {
            var oversized = ArrayPool<byte>.Shared.Rent(PayloadLength);
            value.Body.CopyTo(oversized);
            if (!Payload.Span.SequenceEqual(new(oversized, 0, PayloadLength)))
            {
                throw new InvalidOperationException("Invalid payload");
            }

            ArrayPool<byte>.Shared.Return(oversized);
        }

        if (value.Headers.Length != _headers.Count - 2)
        {
            throw new InvalidOperationException("Incorrect header count");
        }
        foreach (var header in _headers)
        {
            if (header.Key == HeaderNames.ContentLength || header.Key == HeaderNames.RequestId)
            {
                // not stored
                continue;
            }
            if (!value.TryFindHeader(header.Key, out var vals) || vals != header.Value)
            {
                throw new InvalidOperationException("Invalid header: " + header.Key);
            }
        }
    }

    sealed class DummyStore : IOutputCacheStore
    {
        private readonly string _key;
        private byte[]? _payload;
        public DummyStore(string key) => _key = key;

        ValueTask IOutputCacheStore.EvictByTagAsync(string tag, CancellationToken cancellationToken) => default;

        ValueTask<byte[]?> IOutputCacheStore.GetAsync(string key, CancellationToken cancellationToken)
        {
            if (key != _key)
            {
                Throw();
            }
            return new(_payload);
        }

        ValueTask IOutputCacheStore.SetAsync(string key, byte[]? value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
        {
            if (key != _key)
            {
                Throw();
            }
            _payload = value;
            return default;
        }

        static void Throw() => throw new InvalidOperationException("Incorrect key");
    }

    internal sealed class NullPipeWriter : PipeWriter, IDisposable
    {
        public void Dispose()
        {
            var arr = _buffer;
            _buffer = null!;
            if (arr is not null)
            {
                ArrayPool<byte>.Shared.Return(arr);
            }
        }
        byte[] _buffer;
        public NullPipeWriter(int size) => _buffer = ArrayPool<byte>.Shared.Rent(size);
        public override void Advance(int bytes) { }
        public override Span<byte> GetSpan(int sizeHint = 0) => _buffer;
        public override Memory<byte> GetMemory(int sizeHint = 0) => _buffer;
        public override void Complete(Exception? exception = null) { }
        public override void CancelPendingFlush() { }
        public override ValueTask CompleteAsync(Exception? exception = null) => default;
        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => default;
    }
}
