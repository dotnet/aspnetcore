// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Compression;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// DEFLATE decompression provider.
/// </summary>
internal sealed class DeflateDecompressionProvider : IDecompressionProvider
{
    /// <inheritdoc />
    public Stream GetDecompressionStream(Stream stream)
    {
        return new ZLibOrDeflateStream(stream);
    }

    // As described in RFC 2616, the deflate content-coding token represents the "zlib" format
    // (RFC 1950) in combination with the "deflate" compression algorithm (RFC 1951). However,
    // in practice, it is also possible for raw, unwrapped deflate compression to be used with
    // "deflate" as the content-encoding. This class lets us wrap either a zlib- or deflate-
    // compressed stream and delay figuring out what it is until the first read.
    internal sealed class ZLibOrDeflateStream : Stream
    {
        private readonly PeekFirstByteReadStream _stream;
        private Stream? _decompressionStream;

        public ZLibOrDeflateStream(Stream stream) => _stream = new PeekFirstByteReadStream(stream);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _decompressionStream?.Dispose();
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => _stream.CanSeek;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

        // On the first read request, peek at the first nibble of the response. If it's an 8, use ZLibStream, otherwise
        // use DeflateStream. This heuristic works because we're deciding only between raw deflate and zlib wrapped around
        // deflate, in which case the first nibble will always be 8 for zlib and never be 8 for deflate.
        // https://stackoverflow.com/a/37528114 provides an explanation for why.

        public override int Read(Span<byte> buffer)
        {
            if (_decompressionStream is null)
            {
                int firstByte = _stream.PeekFirstByte();
                _decompressionStream = CreateDecompressionStream(firstByte, _stream);
            }

            return _decompressionStream.Read(buffer);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_decompressionStream is null)
            {
                return CreateAndReadAsync(this, buffer, cancellationToken);

                static async ValueTask<int> CreateAndReadAsync(ZLibOrDeflateStream thisRef, Memory<byte> buffer, CancellationToken cancellationToken)
                {
                    int firstByte = await thisRef._stream.PeekFirstByteAsync(cancellationToken).ConfigureAwait(false);
                    thisRef._decompressionStream = CreateDecompressionStream(firstByte, thisRef._stream);
                    return await thisRef._decompressionStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
            }

            return _decompressionStream.ReadAsync(buffer, cancellationToken);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);
            return Core(destination, bufferSize, cancellationToken);
            async Task Core(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                if (_decompressionStream is null)
                {
                    int firstByte = await _stream.PeekFirstByteAsync(cancellationToken).ConfigureAwait(false);
                    _decompressionStream = CreateDecompressionStream(firstByte, _stream);
                }

                await _decompressionStream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
            }
        }

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override void Flush() => _stream.Flush();

        private static Stream CreateDecompressionStream(int firstByte, Stream stream) =>
            (firstByte & 0xF) == 8 ?
                new ZLibStream(stream, CompressionMode.Decompress) :
                new DeflateStream(stream, CompressionMode.Decompress);

        // As the name suggests, this is just a Stream that allows peeking at the first byte.
        private sealed class PeekFirstByteReadStream : Stream
        {
            private readonly Stream _stream;
            private byte _firstByte;
            private FirstByteStatus _firstByteStatus;

            public PeekFirstByteReadStream(Stream stream) => _stream = stream;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _stream.Dispose();
                }

                base.Dispose(disposing);
            }

            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override bool CanSeek => _stream.CanSeek;
            public override long Length => _stream.Length;
            public override long Position { get => _stream.Position; set => _stream.Position = value; }
            public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
            public override void SetLength(long value) => _stream.SetLength(value);
            public override void Flush() => _stream.Flush();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

            public int PeekFirstByte()
            {
                Debug.Assert(_firstByteStatus == FirstByteStatus.None);

                int value = _stream.ReadByte();
                if (value == -1)
                {
                    _firstByteStatus = FirstByteStatus.Consumed;
                    return -1;
                }

                _firstByte = (byte)value;
                _firstByteStatus = FirstByteStatus.Available;
                return value;
            }

            public async ValueTask<int> PeekFirstByteAsync(CancellationToken cancellationToken)
            {
                Debug.Assert(_firstByteStatus == FirstByteStatus.None);

                var buffer = new byte[1];

                int bytesRead = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    _firstByteStatus = FirstByteStatus.Consumed;
                    return -1;
                }

                _firstByte = buffer[0];
                _firstByteStatus = FirstByteStatus.Available;
                return buffer[0];
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                if (_firstByteStatus == FirstByteStatus.Available)
                {
                    if (buffer.Length != 0)
                    {
                        buffer.Span[0] = _firstByte;
                        _firstByteStatus = FirstByteStatus.Consumed;
                        return new ValueTask<int>(1);
                    }

                    return new ValueTask<int>(0);
                }

                Debug.Assert(_firstByteStatus == FirstByteStatus.Consumed);
                return _stream.ReadAsync(buffer, cancellationToken);
            }

            public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                Debug.Assert(_firstByteStatus != FirstByteStatus.None);

                ValidateCopyToArguments(destination, bufferSize);
                if (_firstByteStatus == FirstByteStatus.Available)
                {
                    await destination.WriteAsync(new byte[] { _firstByte }, cancellationToken).ConfigureAwait(false);
                    _firstByteStatus = FirstByteStatus.Consumed;
                }

                await _stream.CopyToAsync(destination, bufferSize, cancellationToken).ConfigureAwait(false);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_firstByteStatus == FirstByteStatus.Available)
                {
                    if (buffer.Length != 0)
                    {
                        buffer[0] = _firstByte;
                        _firstByteStatus = FirstByteStatus.Consumed;
                        return 1;
                    }

                    return 0;
                }

                Debug.Assert(_firstByteStatus == FirstByteStatus.Consumed);
                return _stream.Read(buffer, offset, count);
            }

            private enum FirstByteStatus : byte
            {
                None = 0,
                Available = 1,
                Consumed = 2
            }
        }
    }
}
