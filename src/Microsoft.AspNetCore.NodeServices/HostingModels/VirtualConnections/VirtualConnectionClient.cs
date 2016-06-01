using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.HostingModels.VirtualConnections
{
    public delegate void VirtualConnectionReadErrorHandler(Exception ex);

    /// <summary>
    /// Wraps an underlying physical read/write stream (e.g., named pipes, domain sockets, or TCP sockets) and
    /// exposes an API for making 'virtual connections', which act as independent read/write streams.
    /// Traffic over these virtual connections is multiplexed over the underlying physical stream. This is useful
    /// for fast stream-based inter-process communication because it avoids the overhead of opening a new physical
    /// connection each time a new communication channel is needed.
    /// </summary>
    internal class VirtualConnectionClient : IDisposable
    {
        internal const int MaxFrameBodyLength = 16 * 1024;

        public event VirtualConnectionReadErrorHandler OnError;

        private Stream _underlyingTransport;
        private Dictionary<long, VirtualConnection> _activeInnerStreams;
        private long _nextInnerStreamId;
        private readonly SemaphoreSlim _streamWriterSemaphore = new SemaphoreSlim(1);
        private readonly object _readControlLock = new object();
        private Exception _readLoopExitedWithException;
        private readonly CancellationTokenSource _disposalCancellatonToken = new CancellationTokenSource();
        private bool _disposedValue = false;

        public VirtualConnectionClient(Stream underlyingTransport)
        {
            _underlyingTransport = underlyingTransport;
            _activeInnerStreams = new Dictionary<long, VirtualConnection>();

            RunReadLoop();
        }

        public Stream OpenVirtualConnection()
        {
            // Improve discoverability of read-loop errors (in case the developer doesn't add an OnError listener)
            ThrowIfReadLoopFailed();

            var id = Interlocked.Increment(ref _nextInnerStreamId);
            var newInnerStream = new VirtualConnection(id, this);
            _activeInnerStreams.Add(id, newInnerStream);
            return newInnerStream;
        }

        // It's async void because nothing waits for it to finish (it continues indefinitely). It signals any errors via
        // a separate channel.
        private async void RunReadLoop()
        {
            try
            {
                while (!_disposalCancellatonToken.IsCancellationRequested)
                {
                    var remoteIsStillConnected = await ProcessNextFrameAsync();
                    if (!remoteIsStillConnected)
                    {
                        CloseAllActiveStreams();
                    }
                }
            }
            catch (Exception ex)
            {
                // Not all underlying transports correctly honor cancellation tokens. For example,
                // DomainSocketStreamTransport's ReadAsync ignores them, so we only know to stop
                // the read loop when the underlying stream is disposed and then it throws ObjectDisposedException.
                if (!(ex is TaskCanceledException || ex is ObjectDisposedException))
                {
                    _readLoopExitedWithException = ex;

                    var evt = OnError;
                    if (evt != null)
                    {
                        evt(ex);
                    }
                }
            }
        }

        private async Task<bool> ProcessNextFrameAsync()
        {
            // First read frame header
            var frameHeaderBuffer = await ReadExactLength(12);
            if (frameHeaderBuffer == null)
            {
                return false; // Underlying stream was closed
            }

            // Parse frame header, then read the frame body
            long streamId = BitConverter.ToInt64(frameHeaderBuffer, 0);
            int frameBodyLength = BitConverter.ToInt32(frameHeaderBuffer, 8);
            if (frameBodyLength < 0 || frameBodyLength > MaxFrameBodyLength)
            {
                throw new InvalidDataException("Illegal frame length: " + frameBodyLength);
            }

            var frameBody = await ReadExactLength(frameBodyLength);
            if (frameBody == null)
            {
                return false; // Underlying stream was closed
            }

            // Dispatch the frame to the relevant inner stream
            VirtualConnection innerStream;
            lock (_activeInnerStreams)
            {
                _activeInnerStreams.TryGetValue(streamId, out innerStream);
            }

            if (innerStream != null)
            {
                await innerStream.AddDataToQueue(frameBody);
            }

            return true;
        }

        private async Task<byte[]> ReadExactLength(int lengthToRead) {
            byte[] buffer = new byte[lengthToRead];
            var totalBytesRead = 0;
            var ct = _disposalCancellatonToken.Token;
            while (totalBytesRead < lengthToRead)
            {
                var chunkLengthRead = await _underlyingTransport.ReadAsync(buffer, totalBytesRead, lengthToRead - totalBytesRead, ct);
                if (chunkLengthRead == 0)
                {
                    // Underlying stream was closed
                    return null;
                }

                totalBytesRead += chunkLengthRead;
            }

            return buffer;
        }

        private void CloseAllActiveStreams()
        {
            IList<VirtualConnection> innerStreamsCopy;

            // Only hold the lock while cloning the list of inner streams. Release the lock before
            // actually disposing them, because each 'dispose' call will try to take another lock
            // so it can remove that inner stream from activeInnerStreams.
            lock (_activeInnerStreams)
            {
                innerStreamsCopy = _activeInnerStreams.Values.ToList();
            }

            foreach (var stream in innerStreamsCopy)
            {
                stream.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposedValue)
            {
                _disposedValue = true;

                _disposalCancellatonToken.Cancel(); // Stops the read loop
                CloseAllActiveStreams();
            }
        }

        public async Task WriteAsync(long innerStreamId, byte[] data, int offset, int count, CancellationToken cancellationToken)
        {
            // In case the amount of data to be sent exceeds the max frame length, split it into separate frames
            // Note that we always send at least one frame, even if it's empty, because the zero-length frame is the signal to close a virtual connection
            // (hence 'do..while' instead of just 'while').
            int bytesWritten = 0;
            do {
                // Improve discoverability of read-loop errors (in case the developer doesn't add an OnError listener)
                ThrowIfReadLoopFailed();

                // Hold the write lock only for the time taken to send a single frame, not all frames, to allow large sends to be proceed in parallel
                await _streamWriterSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    // Write stream ID, then length prefix, then chunk payload, then flush
                    var nextChunkBodyLength = Math.Min(MaxFrameBodyLength, count - bytesWritten);
                    await _underlyingTransport.WriteAsync(BitConverter.GetBytes(innerStreamId), 0, 8, cancellationToken).ConfigureAwait(false);
                    await _underlyingTransport.WriteAsync(BitConverter.GetBytes(nextChunkBodyLength), 0, 4, cancellationToken).ConfigureAwait(false);

                    if (nextChunkBodyLength > 0)
                    {
                        await _underlyingTransport.WriteAsync(data, offset + bytesWritten, nextChunkBodyLength, cancellationToken).ConfigureAwait(false);
                        bytesWritten += nextChunkBodyLength;
                    }

                    await _underlyingTransport.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _streamWriterSemaphore.Release();
                }
            } while (bytesWritten < count);
        }

        public void CloseInnerStream(long innerStreamId, bool isAlreadyClosedRemotely)
        {
            lock (_activeInnerStreams)
            {
                if (_activeInnerStreams.ContainsKey(innerStreamId))
                {
                    _activeInnerStreams.Remove(innerStreamId);
                }
            }

            if (!isAlreadyClosedRemotely) {
                // Also notify the remote that this innerstream is closed
                WriteAsync(innerStreamId, new byte[0], 0, 0, new CancellationToken()).Wait();
            }
        }

        private void ThrowIfReadLoopFailed()
        {
            if (_readLoopExitedWithException != null)
            {
                throw new AggregateException("The connection failed - see InnerException for details.", _readLoopExitedWithException);
            }
        }
    }
}