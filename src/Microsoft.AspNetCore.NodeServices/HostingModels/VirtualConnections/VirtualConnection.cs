using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.AspNetCore.NodeServices.HostingModels.VirtualConnections
{
    /// <summary>
    /// A virtual read/write connection, typically to a remote process. Multiple virtual connections can be
    /// multiplexed over a single physical connection (e.g., a named pipe, domain socket, or TCP socket).
    /// </summary>
    internal class VirtualConnection : Stream
    {
#if NET451
        private readonly static Task CompletedTask = Task.FromResult((object)null);
#else
        private readonly static Task CompletedTask = Task.CompletedTask;
#endif
        private VirtualConnectionClient _host;
        private readonly BufferBlock<byte[]> _receivedDataQueue = new BufferBlock<byte[]>();
        private ArraySegment<byte> _receivedDataNotYetUsed;
        private bool _wasClosedByRemote;
        private bool _isDisposed;

        public VirtualConnection(long id, VirtualConnectionClient host)
        {
            Id = id;
            _host = host;
        }

        public long Id { get; }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return true; } }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Flush()
        {
            // We're auto-flushing, so this is a no-op.
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_wasClosedByRemote)
            {
                return 0;
            }

            var bytesRead = 0;
            while (true)
            {
                // Pull as many applicable bytes as we can out of receivedDataNotYetUsed, then update its offset/length
                int bytesToExtract = Math.Min(count - bytesRead, _receivedDataNotYetUsed.Count);
                if (bytesToExtract > 0)
                {
                    Buffer.BlockCopy(_receivedDataNotYetUsed.Array, _receivedDataNotYetUsed.Offset, buffer, bytesRead, bytesToExtract);
                    _receivedDataNotYetUsed = new ArraySegment<byte>(_receivedDataNotYetUsed.Array, _receivedDataNotYetUsed.Offset + bytesToExtract, _receivedDataNotYetUsed.Count - bytesToExtract);
                    bytesRead += bytesToExtract;
                }

                // If we've completely filled the output buffer, we're done
                if (bytesRead == count)
                {
                    return bytesRead;
                }

                // We haven't yet filled the output buffer, so we must have exhausted receivedDataNotYetUsed instead.
                // We want to get the next block of data from the underlying queue.
                byte[] nextReceivedBlock;
                if (bytesRead > 0)
                {
                    if (!_receivedDataQueue.TryReceive(null, out nextReceivedBlock))
                    {
                        // No more data is available synchronously, and we already have some data, so we can stop now
                        return bytesRead;
                    }
                }
                else
                {
                    // Since we don't yet have anything, wait for the underlying source
                    nextReceivedBlock = await _receivedDataQueue.ReceiveAsync(cancellationToken);
                }

                if (nextReceivedBlock.Length == 0)
                {
                    // A zero-length block signals that the remote regards this virtual connection as closed
                    _wasClosedByRemote = true;
                    return bytesRead;
                }
                else
                {
                    // We got some more data, so can continue trying to fill the output buffer
                    _receivedDataNotYetUsed = new ArraySegment<byte>(nextReceivedBlock, 0, nextReceivedBlock.Length);
                }
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_wasClosedByRemote)
            {
                throw new InvalidOperationException("The connection was already closed by the remote party");
            }

            return count > 0 ? _host.WriteAsync(Id, buffer, offset, count, cancellationToken) : CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, CancellationToken.None).Wait();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                _host.CloseInnerStream(Id, _wasClosedByRemote);
            }
        }

        public async Task AddDataToQueue(byte[] data)
        {
            await _receivedDataQueue.SendAsync(data);
        }
    }
}