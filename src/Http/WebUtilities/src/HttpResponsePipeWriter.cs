// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Writes to the <see cref="PipeWriter"/> using the supplied <see cref="Encoding"/>.
    /// It does not write the BOM and also does not close the stream.
    /// </summary>
    public class HttpResponsePipeWriter : TextWriter
    {
        private readonly Encoder _encoder;
        private readonly char[] _singleCharArray;
        private readonly PipeWriter _writer;

        public override Encoding Encoding { get; }

        private int _uncommittedBytes = 0;
        private bool _disposed;

        public HttpResponsePipeWriter(
            PipeWriter writer,
            Encoding encoding)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _encoder = encoding.GetEncoder();
            _singleCharArray = ArrayPool<char>.Shared.Rent(1);
        }

        public override void Write(char value)
        {
            _singleCharArray[0] = value;
            WriteInternal(_singleCharArray.AsSpan(0, 1));
        }

        public override void Write(char[] values, int index, int count)
        {
            if (values == null)
            {
                return;
            }

            WriteInternal(values.AsSpan(index, count));
        }

        public override void Write(ReadOnlySpan<char> value)
        {
            if (value == null)
            {
                return;
            }

            WriteInternal(value);
        }

        public override void Write(string? value)
        {
            if (value == null)
            {
                return;
            }

            WriteInternal(value.AsSpan());
        }

        public override void WriteLine(ReadOnlySpan<char> value)
            => WriteInternal(value, addNewLine: true);

        public override Task WriteAsync(char value)
        {
            _singleCharArray[0] = value;

            return WriteInternalAsync(_singleCharArray.AsSpan(0, 1));
        }

        public override Task WriteAsync(char[] values, int index, int count)
            => WriteInternalAsync(values.AsSpan(index, count));

        public override Task WriteAsync(string? value)
        {
            if (value == null)
            {
                return Task.CompletedTask;
            }

            return WriteInternalAsync(value.AsSpan());
        }

        [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Required to maintain compatibility")]
        public override Task WriteAsync(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
            => WriteInternalAsync(value.Span, cancellationToken);

        public override Task WriteLineAsync(ReadOnlyMemory<char> value, CancellationToken cancellationToken = default)
            => WriteInternalAsync(value.Span, cancellationToken, addNewLine: true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task WriteInternalAsync(ReadOnlySpan<char> value, CancellationToken cancellationToken = default, bool addNewLine = false)
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (value.IsEmpty && !addNewLine)
            {
                return Task.CompletedTask;
            }

            WriteInternal(value, addNewLine);

            return LazyFlushAsync(cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Task LazyFlushAsync(CancellationToken cancellationToken = default)
        {
            // The max size of a chunk is 4089.
            if (_uncommittedBytes >= 4089)
            {
                _uncommittedBytes = 0;
                return _writer.FlushAsync(cancellationToken).AsTask();
            }

            return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteInternal(ReadOnlySpan<char> value, bool addNewLine = false)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            WriteSpan(value);
            if (addNewLine) WriteSpan(NewLine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSpan(ReadOnlySpan<char> value)
        {
            var length = _encoder.GetByteCount(value, false);
            var buffer = _writer.GetSpan(length);
            _uncommittedBytes += length;
            _encoder.GetBytes(value, buffer, false);
            _writer.Advance(length);
        }

        public override void Flush()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpResponseStreamWriter));
            }

            FlushEncoder();
            // TOOD: flush?
        }

        // Perf: FlushAsync is invoked to ensure any buffered content is asynchronously written to the underlying
        // response asynchronously. In its absence, the buffer gets synchronously written to the
        // response as part of the Dispose which has a perf impact.
        public override Task FlushAsync()
        {
            if (_disposed)
            {
                return GetObjectDisposedTask();
            }

            return FlushInternalAsync().AsTask();
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                await FlushInternalAsync();
            }

            await base.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                FlushEncoder();
                // Flush not needed
            }

            base.Dispose(disposing);
        }

        private ValueTask<FlushResult> FlushInternalAsync()
        {
            FlushEncoder();
            return _writer.FlushAsync();
        }

        private void FlushEncoder()
        {
            var empty = new ReadOnlySpan<char>();
            var length = _encoder.GetByteCount(empty, true);
            if (length > 0)
            {
                var span = _writer.GetSpan(length);
                _encoder.GetBytes(empty, span, true);
                _writer.Advance(length);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Task GetObjectDisposedTask()
        {
            return Task.FromException(new ObjectDisposedException(nameof(HttpResponsePipeWriter)));
        }
    }
}
