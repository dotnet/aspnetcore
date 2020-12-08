// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class RequestStream : Stream, IValueTaskSource<(uint, uint)>
    {
        private ManualResetValueTaskSourceCore<(uint, uint)> _mrvts = new();

        private const int MaxReadSize = 0x20000; // http.sys recommends we limit reads to 128k

        private readonly RequestStreamOverlapped _overlapped;
        private readonly RequestContext _requestContext;

        private long? _maxSize;
        private long _totalRead;
        private bool _closed;

        internal RequestStream(RequestContext httpContext)
        {
            _requestContext = httpContext;
            _maxSize = _requestContext.Server.Options.MaxRequestBodySize;
            _overlapped = new RequestStreamOverlapped(this);
        }

        internal RequestContext RequestContext
        {
            get { return _requestContext; }
        }

        private SafeHandle RequestQueueHandle => RequestContext.Server.RequestQueue.Handle;

        private ulong RequestId => RequestContext.Request.RequestId;

        private ILogger Logger => RequestContext.Server.Logger;

        public bool HasStarted { get; private set; }

        public long? MaxSize
        {
            get => _maxSize;
            set
            {
                if (HasStarted)
                {
                    throw new InvalidOperationException("The maximum request size cannot be changed after the request body has started reading.");
                }
                if (value.HasValue && value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be greater or equal to zero.");
                }
                _maxSize = value;
            }
        }

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override long Length => throw new NotSupportedException(Resources.Exception_NoSeek);

        public override long Position
        {
            get => throw new NotSupportedException(Resources.Exception_NoSeek);
            set => throw new NotSupportedException(Resources.Exception_NoSeek);
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException(Resources.Exception_NoSeek);

        public override void SetLength(long value) => throw new NotSupportedException(Resources.Exception_NoSeek);

        public override void Flush() => throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);

        public override Task FlushAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);

        internal void SwitchToOpaqueMode()
        {
            HasStarted = true;
            _maxSize = null;
        }

        internal void Abort()
        {
            _closed = true;
            _requestContext.Abort();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!RequestContext.AllowSynchronousIO)
            {
                throw new InvalidOperationException("Synchronous IO APIs are disabled, see AllowSynchronousIO.");
            }

            ValidateBufferArguments(buffer, offset, count);
            CheckSizeLimit();

            if (_closed)
            {
                return 0;
            }

            return ReadCore(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            if (!RequestContext.AllowSynchronousIO)
            {
                throw new InvalidOperationException("Synchronous IO APIs are disabled, see AllowSynchronousIO.");
            }

            CheckSizeLimit();

            if (_closed)
            {
                return 0;
            }

            return ReadCore(buffer);
        }

        private unsafe int ReadCore(Span<byte> buffer)
        {
            // the http.sys team recommends that we limit the size to 128kb
            if (buffer.Length > MaxReadSize)
            {
                buffer = buffer.Slice(0, MaxReadSize);
            }

            uint statusCode;
            uint dataRead;

            fixed (byte* pBuffer = buffer)
            {
                // issue unmanaged blocking call
                statusCode =
                    HttpApi.HttpReceiveRequestEntityBody(
                        RequestQueueHandle,
                        RequestId,
                        flags: 0,
                        (IntPtr)pBuffer,
                        (uint)buffer.Length,
                        out dataRead,
                        null);
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
            {
                Exception exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                Logger.LogError(LoggerEventIds.ErrorWhileRead, exception, "Read");
                Abort();
                throw exception;
            }

            UpdateAfterRead(statusCode, dataRead);

            if (TryCheckSizeLimit((int)dataRead, out var ex))
            {
                throw ex;
            }

            // TODO: Verbose log dump data read
            return (int)dataRead;
        }

        internal void UpdateAfterRead(uint statusCode, uint dataRead)
        {
            // REVIEW: We should avoid auto disposing like this
            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF || dataRead == 0)
            {
                Dispose();
            }
        }

        public override unsafe IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            CheckSizeLimit();
            if (_closed)
            {
                return Task.FromResult(0);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            return ReadAsyncCore(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckSizeLimit();
            if (_closed)
            {
                return ValueTask.FromResult(0);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            return ReadAsyncCore(buffer, cancellationToken);
        }

        public async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // the http.sys team recommends that we limit the size to 128kb
            if (buffer.Length > MaxReadSize)
            {
                buffer = buffer.Slice(0, MaxReadSize);
            }

            var cancellationRegistration = default(CancellationTokenRegistration);
            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = RequestContext.RegisterForCancellation(cancellationToken);
            }

            uint dataRead = 0;
            uint statusCode = 0;

            // Pin the user buffer
            var handle = buffer.Pin();

            try
            {
                using (cancellationRegistration)
                {
                    (statusCode, dataRead) = await ReadEntityBodyAsync(handle, buffer.Length);
                }
            }
            catch (Exception e)
            {
                Abort();
                Logger.LogError(LoggerEventIds.ErrorWhenReadAsync, e, "ReadAsync");
                throw;
            }
            finally
            {
                handle.Dispose();
            }

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS && statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_HANDLE_EOF)
            {
                var exception = new IOException(string.Empty, new HttpSysException((int)statusCode));
                Logger.LogError(LoggerEventIds.ErrorWhenReadAsync, exception, "ReadAsync");
                Abort();
                throw exception;
            }

            UpdateAfterRead(statusCode, dataRead);

            if (TryCheckSizeLimit((int)dataRead, out var ex))
            {
                throw ex;
            }

            // TODO: Verbose log dump data read
            return (int)dataRead;
        }

        private ValueTask<(uint, uint)> ReadEntityBodyAsync(MemoryHandle handle, int length)
        {
            uint dataRead;
            uint statusCode;

            unsafe
            {
                statusCode = HttpApi.HttpReceiveRequestEntityBody(
                        RequestQueueHandle,
                        RequestId,
                        flags: 0,
                        (IntPtr)handle.Pointer,
                        (uint)length,
                        out dataRead,
                        _overlapped.NativeOverlapped);
            }

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_IO_PENDING)
            {
                _mrvts.Reset();

                return new ValueTask<(uint, uint)>(this, _mrvts.Version);
            }

            return ValueTask.FromResult((statusCode, dataRead));
        }

        public override void Write(byte[] buffer, int offset, int size)
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new InvalidOperationException(Resources.Exception_ReadOnlyStream);
        }

        // Called before each read
        private void CheckSizeLimit()
        {
            // Note SwitchToOpaqueMode sets HasStarted and clears _maxSize, so these limits don't apply.
            if (!HasStarted)
            {
                var contentLength = RequestContext.Request.ContentLength;
                if (contentLength.HasValue && _maxSize.HasValue && contentLength.Value > _maxSize.Value)
                {
                    throw new BadHttpRequestException(
                        $"The request's Content-Length {contentLength.Value} is larger than the request body size limit {_maxSize.Value}.",
                        StatusCodes.Status413PayloadTooLarge);
                }

                HasStarted = true;
            }
            else if (TryCheckSizeLimit(0, out var exception))
            {
                throw exception;
            }
        }

        // Called after each read.
        private bool TryCheckSizeLimit(int bytesRead, out Exception exception)
        {
            _totalRead += bytesRead;
            if (_maxSize.HasValue && _totalRead > _maxSize.Value)
            {
                exception = new BadHttpRequestException(
                    $"The total number of bytes read {_totalRead} has exceeded the request body size limit {_maxSize.Value}.",
                    StatusCodes.Status413PayloadTooLarge);
                return true;
            }
            exception = null;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _closed = true;

                _overlapped.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void OnIOCompleted(uint numBytes, uint errorCode)
        {
            _mrvts.SetResult((errorCode, numBytes));
        }

        (uint, uint) IValueTaskSource<(uint, uint)>.GetResult(short token)
        {
            return _mrvts.GetResult(token);
        }

        ValueTaskSourceStatus IValueTaskSource<(uint, uint)>.GetStatus(short token)
        {
            return _mrvts.GetStatus(token);
        }

        void IValueTaskSource<(uint, uint)>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            _mrvts.OnCompleted(continuation, state, token, flags);
        }

        // This can't be the same class as the stream because it's already deriving from Stream
        private unsafe class RequestStreamOverlapped : Overlapped, IDisposable
        {
            private static readonly IOCompletionCallback IOCompletionCallback = static (uint errorCode, uint numBytes, NativeOverlapped* pOVERLAP) =>
            {
                var overlapped = (RequestStreamOverlapped)Unpack(pOVERLAP);
                overlapped.Stream.OnIOCompleted(numBytes, errorCode);
            };

            private bool _disposed;

            public RequestStreamOverlapped(RequestStream requestStream)
            {
                Stream = requestStream;
                NativeOverlapped = UnsafePack(IOCompletionCallback, userData: null);
            }

            public RequestStream Stream { get; }

            public NativeOverlapped* NativeOverlapped { get; }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    Free(NativeOverlapped);
                    _disposed = true;
                }
            }

            ~RequestStreamOverlapped()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
