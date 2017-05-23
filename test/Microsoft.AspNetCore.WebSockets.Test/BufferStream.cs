// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebSockets.Test
{
    // This steam accepts writes from one side, buffers them internally, and returns the data via Reads
    // when requested on the other side.
    public class BufferStream : Stream
    {
        private bool _disposed;
        private bool _aborted;
        private bool _terminated;
        private Exception _abortException;
        private ConcurrentQueue<byte[]> _bufferedData;
        private ArraySegment<byte> _topBuffer;
        private SemaphoreSlim _readLock;
        private SemaphoreSlim _writeLock;
        private TaskCompletionSource<object> _readWaitingForData;

        internal BufferStream()
        {
            _readLock = new SemaphoreSlim(1, 1);
            _writeLock = new SemaphoreSlim(1, 1);
            _bufferedData = new ConcurrentQueue<byte[]>();
            _readWaitingForData = new TaskCompletionSource<object>();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        #region NotSupported

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        #endregion NotSupported

        /// <summary>
        /// Ends the stream, meaning all future reads will return '0'.
        /// </summary>
        public void End()
        {
            _terminated = true;
        }

        public override void Flush()
        {
            CheckDisposed();
            // TODO: Wait for data to drain?
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            Flush();

            // TODO: Wait for data to drain?

            return Task.FromResult(0);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(_terminated)
            {
                return 0;
            }

            VerifyBuffer(buffer, offset, count, allowEmpty: false);
            _readLock.Wait();
            try
            {
                int totalRead = 0;
                do
                {
                    // Don't drain buffered data when signaling an abort.
                    CheckAborted();
                    if (_topBuffer.Count <= 0)
                    {
                        byte[] topBuffer = null;
                        while (!_bufferedData.TryDequeue(out topBuffer))
                        {
                            if (_disposed)
                            {
                                CheckAborted();
                                // Graceful close
                                return totalRead;
                            }
                            WaitForDataAsync().Wait();
                        }
                        _topBuffer = new ArraySegment<byte>(topBuffer);
                    }
                    int actualCount = Math.Min(count, _topBuffer.Count);
                    Buffer.BlockCopy(_topBuffer.Array, _topBuffer.Offset, buffer, offset, actualCount);
                    _topBuffer = new ArraySegment<byte>(_topBuffer.Array,
                        _topBuffer.Offset + actualCount,
                        _topBuffer.Count - actualCount);
                    totalRead += actualCount;
                    offset += actualCount;
                    count -= actualCount;
                }
                while (count > 0 && (_topBuffer.Count > 0 || _bufferedData.Count > 0));
                // Keep reading while there is more data available and we have more space to put it in.
                return totalRead;
            }
            finally
            {
                _readLock.Release();
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            // TODO: This option doesn't preserve the state object.
            // return ReadAsync(buffer, offset, count);
            return base.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            // return ((Task<int>)asyncResult).Result;
            return base.EndRead(asyncResult);
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_terminated)
            {
                return 0;
            }

            VerifyBuffer(buffer, offset, count, allowEmpty: false);
            var registration = cancellationToken.Register(Abort);
            await _readLock.WaitAsync(cancellationToken);
            try
            {
                int totalRead = 0;
                do
                {
                    // Don't drained buffered data on abort.
                    CheckAborted();
                    if (_topBuffer.Count <= 0)
                    {
                        byte[] topBuffer = null;
                        while (!_bufferedData.TryDequeue(out topBuffer))
                        {
                            if (_disposed)
                            {
                                CheckAborted();
                                // Graceful close
                                return totalRead;
                            }
                            await WaitForDataAsync();
                        }
                        _topBuffer = new ArraySegment<byte>(topBuffer);
                    }
                    var actualCount = Math.Min(count, _topBuffer.Count);
                    Buffer.BlockCopy(_topBuffer.Array, _topBuffer.Offset, buffer, offset, actualCount);
                    _topBuffer = new ArraySegment<byte>(_topBuffer.Array,
                        _topBuffer.Offset + actualCount,
                        _topBuffer.Count - actualCount);
                    totalRead += actualCount;
                    offset += actualCount;
                    count -= actualCount;
                }
                while (count > 0 && (_topBuffer.Count > 0 || _bufferedData.Count > 0));
                // Keep reading while there is more data available and we have more space to put it in.
                return totalRead;
            }
            finally
            {
                registration.Dispose();
                _readLock.Release();
            }
        }

        // Write with count 0 will still trigger OnFirstWrite
        public override void Write(byte[] buffer, int offset, int count)
        {
            VerifyBuffer(buffer, offset, count, allowEmpty: true);
            CheckDisposed();

            _writeLock.Wait();
            try
            {
                if (count == 0)
                {
                    return;
                }
                // Copies are necessary because we don't know what the caller is going to do with the buffer afterwards.
                var internalBuffer = new byte[count];
                Buffer.BlockCopy(buffer, offset, internalBuffer, 0, count);
                _bufferedData.Enqueue(internalBuffer);

                SignalDataAvailable();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            Write(buffer, offset, count);
            var tcs = new TaskCompletionSource<object>(state);
            tcs.TrySetResult(null);
            var result = tcs.Task;
            if (callback != null)
            {
                callback(result);
            }
            return result;
        }

        public override void EndWrite(IAsyncResult asyncResult) { }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            VerifyBuffer(buffer, offset, count, allowEmpty: true);
            if (cancellationToken.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            Write(buffer, offset, count);
            return Task.FromResult<object>(null);
        }

        private static void VerifyBuffer(byte[] buffer, int offset, int count, bool allowEmpty)
        {
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }
            if (count < 0 || count > buffer.Length - offset
                || (!allowEmpty && count == 0))
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
            }
        }

        private void SignalDataAvailable()
        {
            // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
            Task.Factory.StartNew(() => _readWaitingForData.TrySetResult(null));
        }

        private Task WaitForDataAsync()
        {
            _readWaitingForData = new TaskCompletionSource<object>();

            if (!_bufferedData.IsEmpty || _disposed)
            {
                // Race, data could have arrived before we created the TCS.
                _readWaitingForData.TrySetResult(null);
            }

            return _readWaitingForData.Task;
        }

        internal void Abort()
        {
            Abort(new OperationCanceledException());
        }

        internal void Abort(Exception innerException)
        {
            Contract.Requires(innerException != null);
            _aborted = true;
            _abortException = innerException;
            Dispose();
        }

        private void CheckAborted()
        {
            if (_aborted)
            {
                throw new IOException(string.Empty, _abortException);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_writeLock", Justification = "ODEs from the locks would mask IOEs from abort.")]
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_readLock", Justification = "Data can still be read unless we get aborted.")]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Throw for further writes, but not reads.  Allow reads to drain the buffered data and then return 0 for further reads.
                _disposed = true;
                _readWaitingForData.TrySetResult(null);
            }

            base.Dispose(disposing);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
