// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicStream : QuicStreamProvider
    {
        // Pointer to the underlying stream
        // TODO replace all IntPtr with SafeHandles
        private readonly IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        internal static readonly StreamCallbackDelegate s_streamDelegate = new StreamCallbackDelegate(NativeCallbackHandler);

        // Backing for StreamId
        private long _streamId = -1;

        // Resettable completions to be used for multiple calls to send, start, and shutdown.
        private readonly ResettableCompletionSource<uint> _sendResettableCompletionSource;

        // Resettable completions to be used for multiple calls to receive.
        private readonly ResettableCompletionSource<uint> _receiveResettableCompletionSource;

        private readonly ResettableCompletionSource<uint> _shutdownWriteResettableCompletionSource;

        // Buffers to hold during a call to send.
        private MemoryHandle[] _bufferArrays = new MemoryHandle[1];
        private QuicBuffer[] _sendQuicBuffers = new QuicBuffer[1];

        // Handle to hold when sending.
        private GCHandle _sendHandle;

        // Used to check if StartAsync has been called.
        private bool _started;

        private ReadState _readState;
        private long _readErrorCode = -1;

        private ShutdownWriteState _shutdownState;

        private SendState _sendState;
        private long _sendErrorCode = -1;

        // Used by the class to indicate that the stream is m_Readable.
        private readonly bool _canRead;

        // Used by the class to indicate that the stream is writable.
        private readonly bool _canWrite;

        private volatile bool _disposed;

        private readonly List<QuicBuffer> _receiveQuicBuffers = new List<QuicBuffer>();

        // TODO consider using Interlocked.Exchange instead of a sync if we can avoid it.
        private readonly object _sync = new object();

        // Creates a new MsQuicStream
        internal MsQuicStream(MsQuicConnection connection, QUIC_STREAM_OPEN_FLAG flags, IntPtr nativeObjPtr, bool inbound)
        {
            Debug.Assert(connection != null);

            _ptr = nativeObjPtr;

            _sendResettableCompletionSource = new ResettableCompletionSource<uint>();
            _receiveResettableCompletionSource = new ResettableCompletionSource<uint>();
            _shutdownWriteResettableCompletionSource = new ResettableCompletionSource<uint>();
            SetCallbackHandler();

            bool isBidirectional = !flags.HasFlag(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);

            if (inbound)
            {
                _canRead = true;
                _canWrite = isBidirectional;
                _started = true;
            }
            else
            {
                _canRead = isBidirectional;
                _canWrite = true;
                StartLocalStream();
            }
        }

        internal override bool CanRead => _canRead;

        internal override bool CanWrite => _canWrite;

        internal override long StreamId
        {
            get
            {
                ThrowIfDisposed();

                if (_streamId == -1)
                {
                    _streamId = GetStreamId();
                }

                return _streamId;
            }
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffer, endStream: false, cancellationToken);
        }

        internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, endStream: false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using CancellationTokenRegistration registration = await HandleWriteStartState(cancellationToken).ConfigureAwait(false);

            await SendReadOnlySequenceAsync(buffers, endStream ? QUIC_SEND_FLAG.FIN : QUIC_SEND_FLAG.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default)
        {
            return WriteAsync(buffers, endStream: false, cancellationToken);
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using CancellationTokenRegistration registration = await HandleWriteStartState(cancellationToken).ConfigureAwait(false);

            await SendReadOnlyMemoryListAsync(buffers, endStream ? QUIC_SEND_FLAG.FIN : QUIC_SEND_FLAG.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using CancellationTokenRegistration registration = await HandleWriteStartState(cancellationToken).ConfigureAwait(false);

            await SendReadOnlyMemoryAsync(buffer, endStream ? QUIC_SEND_FLAG.FIN : QUIC_SEND_FLAG.NONE).ConfigureAwait(false);

            HandleWriteCompletedState();
        }

        private async ValueTask<CancellationTokenRegistration> HandleWriteStartState(CancellationToken cancellationToken)
        {
            if (!_canWrite)
            {
                throw new InvalidOperationException("Writing is not allowed on stream.");
            }

            lock (_sync)
            {
                if (_sendState == SendState.Aborted)
                {
                    throw new OperationCanceledException("Sending has already been aborted on the stream");
                }
            }

            CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                bool shouldComplete = false;
                lock (_sync)
                {
                    if (_sendState == SendState.None)
                    {
                        _sendState = SendState.Aborted;
                        shouldComplete = true;
                    }
                }

                if (shouldComplete)
                {
                    _sendResettableCompletionSource.CompleteException(new OperationCanceledException("Write was canceled", cancellationToken));
                }
            });

            // Make sure start has completed
            if (!_started)
            {
                await _sendResettableCompletionSource.GetTypelessValueTask().ConfigureAwait(false);
                _started = true;
            }

            return registration;
        }

        private void HandleWriteCompletedState()
        {
            lock (_sync)
            {
                if (_sendState == SendState.Finished)
                {
                    _sendState = SendState.None;
                }
            }
        }

        internal override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!_canRead)
            {
                throw new InvalidOperationException("Reading is not allowed on stream.");
            }

            lock (_sync)
            {
                if (_readState == ReadState.ReadsCompleted)
                {
                    return 0;
                }
                else if (_readState == ReadState.Aborted)
                {
                    throw _readErrorCode switch
                    {
                        -1 => new QuicOperationAbortedException(),
                        long err => new QuicStreamAbortedException(err)
                    };
                }
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                bool shouldComplete = false;
                lock (_sync)
                {
                    if (_readState == ReadState.None)
                    {
                        shouldComplete = true;
                    }

                    _readState = ReadState.Aborted;
                }

                if (shouldComplete)
                {
                    _receiveResettableCompletionSource.CompleteException(new OperationCanceledException("Read was canceled", cancellationToken));
                }
            });

            // TODO there could potentially be a perf gain by storing the buffer from the inital read
            // This reduces the amount of async calls, however it makes it so MsQuic holds onto the buffers
            // longer than it needs to. We will need to benchmark this.
            int length = (int)await _receiveResettableCompletionSource.GetValueTask().ConfigureAwait(false);

            int actual = Math.Min(length, destination.Length);

            static unsafe void CopyToBuffer(Span<byte> destinationBuffer, List<QuicBuffer> sourceBuffers)
            {
                Span<byte> slicedBuffer = destinationBuffer;
                for (int i = 0; i < sourceBuffers.Count; i++)
                {
                    QuicBuffer nativeBuffer = sourceBuffers[i];
                    int length = Math.Min((int)nativeBuffer.Length, slicedBuffer.Length);
                    new Span<byte>(nativeBuffer.Buffer, length).CopyTo(slicedBuffer);
                    if (length < nativeBuffer.Length)
                    {
                        // The buffer passed in was larger that the received data, return
                        return;
                    }
                    slicedBuffer = slicedBuffer.Slice(length);
                }
            }

            CopyToBuffer(destination.Span, _receiveQuicBuffers);

            lock (_sync)
            {
                if (_readState == ReadState.IndividualReadComplete)
                {
                    _receiveQuicBuffers.Clear();
                    ReceiveComplete(actual);
                    EnableReceive();
                    _readState = ReadState.None;
                }
            }

            return actual;
        }

        // TODO do we want this to be a synchronization mechanism to cancel a pending read
        // If so, we need to complete the read here as well.
        internal override void AbortRead(long errorCode)
        {
            ThrowIfDisposed();

            lock (_sync)
            {
                _readState = ReadState.Aborted;
            }

            MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT_RECV, errorCode);

        }

        internal override void AbortWrite(long errorCode)
        {
            ThrowIfDisposed();

            bool shouldComplete = false;

            lock (_sync)
            {
                if (_shutdownState == ShutdownWriteState.None)
                {
                    _shutdownState = ShutdownWriteState.Canceled;
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _shutdownWriteResettableCompletionSource.CompleteException(new QuicStreamAbortedException("Shutdown was aborted.", errorCode));
            }

            MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT_SEND, errorCode);
        }

        internal override ValueTask ShutdownWriteCompleted(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            // TODO do anything to stop writes?
            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                bool shouldComplete = false;
                lock (_sync)
                {
                    if (_shutdownState == ShutdownWriteState.None)
                    {
                        _shutdownState = ShutdownWriteState.Canceled;
                        shouldComplete = true;
                    }
                }

                if (shouldComplete)
                {
                    _shutdownWriteResettableCompletionSource.CompleteException(new OperationCanceledException("Shutdown was canceled", cancellationToken));
                }
            });

            return _shutdownWriteResettableCompletionSource.GetTypelessValueTask();
        }

        internal override void Shutdown()
        {
            ThrowIfDisposed();

            MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, errorCode: 0);
        }

        // TODO consider removing sync-over-async with blocking calls.
        internal override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();

            return ReadAsync(buffer.ToArray()).AsTask().GetAwaiter().GetResult();
        }

        internal override void Write(ReadOnlySpan<byte> buffer)
        {
            ThrowIfDisposed();

            // TODO: optimize this.
            WriteAsync(buffer.ToArray()).AsTask().GetAwaiter().GetResult();
        }

        // MsQuic doesn't support explicit flushing
        internal override void Flush()
        {
            ThrowIfDisposed();
        }

        // MsQuic doesn't support explicit flushing
        internal override Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }

            CleanupSendState();

            if (_ptr != IntPtr.Zero)
            {
                // TODO resolve graceful vs abortive dispose here. Will file a separate issue.
                //MsQuicApi.Api._streamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 1);
                MsQuicApi.Api.StreamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();

            _disposed = true;

            return default;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicStream()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            CleanupSendState();

            if (_ptr != IntPtr.Zero)
            {
                // TODO resolve graceful vs abortive dispose here. Will file a separate issue.
                //MsQuicApi.Api._streamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 1);
                MsQuicApi.Api.StreamCloseDelegate?.Invoke(_ptr);
            }

            _handle.Free();

            _disposed = true;
        }

        private void EnableReceive()
        {
            MsQuicApi.Api.StreamReceiveSetEnabledDelegate(_ptr, enabled: true);
        }

        internal static uint NativeCallbackHandler(
            IntPtr stream,
            IntPtr context,
            ref StreamEvent streamEvent)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicStream = (MsQuicStream)handle.Target!;

            return quicStream.HandleEvent(ref streamEvent);
        }

        private uint HandleEvent(ref StreamEvent evt)
        {
            uint status = MsQuicStatusCodes.Success;

            try
            {
                switch (evt.Type)
                {
                    // Stream has started.
                    // Will only be done for outbound streams (inbound streams have already started)
                    case QUIC_STREAM_EVENT.START_COMPLETE:
                        status = HandleStartComplete();
                        break;
                    // Received data on the stream
                    case QUIC_STREAM_EVENT.RECEIVE:
                        {
                            status = HandleEventRecv(ref evt);
                        }
                        break;
                    // Send has completed.
                    // Contains a canceled bool to indicate if the send was canceled.
                    case QUIC_STREAM_EVENT.SEND_COMPLETE:
                        {
                            status = HandleEventSendComplete(ref evt);
                        }
                        break;
                    // Peer has told us to shutdown the reading side of the stream.
                    case QUIC_STREAM_EVENT.PEER_SEND_SHUTDOWN:
                        {
                            status = HandleEventPeerSendShutdown();
                        }
                        break;
                    // Peer has told us to abort the reading side of the stream.
                    case QUIC_STREAM_EVENT.PEER_SEND_ABORTED:
                        {
                            status = HandleEventPeerSendAborted(ref evt);
                        }
                        break;
                    // Peer has stopped receiving data, don't send anymore.
                    case QUIC_STREAM_EVENT.PEER_RECEIVE_ABORTED:
                        {
                            status = HandleEventPeerRecvAborted(ref evt);
                        }
                        break;
                    // Occurs when shutdown is completed for the send side.
                    // This only happens for shutdown on sending, not receiving
                    // Receive shutdown can only be abortive.
                    case QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventSendShutdownComplete(ref evt);
                        }
                        break;
                    // Shutdown for both sending and receiving is completed.
                    case QUIC_STREAM_EVENT.SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventShutdownComplete();
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                return MsQuicStatusCodes.InternalError;
            }

            return status;
        }

        private unsafe uint HandleEventRecv(ref MsQuicNativeMethods.StreamEvent evt)
        {
            StreamEventDataRecv receieveEvent = evt.Data.Recv;
            for (int i = 0; i < receieveEvent.BufferCount; i++)
            {
                _receiveQuicBuffers.Add(receieveEvent.Buffers[i]);
            }

            bool shouldComplete = false;
            lock (_sync)
            {
                if (_readState == ReadState.None)
                {
                    shouldComplete = true;
                }
                _readState = ReadState.IndividualReadComplete;
            }

            if (shouldComplete)
            {
                _receiveResettableCompletionSource.Complete((uint)receieveEvent.TotalBufferLength);
            }

            return MsQuicStatusCodes.Pending;
        }

        private uint HandleEventPeerRecvAborted(ref StreamEvent evt)
        {
            bool shouldComplete = false;
            lock (_sync)
            {
                if (_sendState == SendState.None)
                {
                    shouldComplete = true;
                }
                _sendState = SendState.Aborted;
                _sendErrorCode = evt.Data.PeerSendAbort.ErrorCode;
            }

            if (shouldComplete)
            {
                _sendResettableCompletionSource.CompleteException(new QuicStreamAbortedException(_sendErrorCode));
            }

            return MsQuicStatusCodes.Success;
        }

        private uint HandleStartComplete()
        {
            bool shouldComplete = false;
            lock (_sync)
            {
                // Check send state before completing as send cancellation is shared between start and send.
                if (_sendState == SendState.None)
                {
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _sendResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventSendShutdownComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            bool shouldComplete = false;
            lock (_sync)
            {
                if (_shutdownState == ShutdownWriteState.None)
                {
                    _shutdownState = ShutdownWriteState.Finished;
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _shutdownWriteResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownComplete()
        {
            bool shouldReadComplete = false;
            bool shouldShutdownWriteComplete = false;

            lock (_sync)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info("Completing resettable event source.");

                if (_readState == ReadState.None)
                {
                    shouldReadComplete = true;
                }

                _readState = ReadState.ReadsCompleted;

                if (_shutdownState == ShutdownWriteState.None)
                {
                    _shutdownState = ShutdownWriteState.Finished;
                    shouldShutdownWriteComplete = true;
                }
            }

            if (shouldReadComplete)
            {
                _receiveResettableCompletionSource.Complete(0);
            }

            if (shouldShutdownWriteComplete)
            {
                _shutdownWriteResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventPeerSendAborted(ref StreamEvent evt)
        {
            bool shouldComplete = false;
            lock (_sync)
            {
                if (_readState == ReadState.None)
                {
                    shouldComplete = true;
                }
                _readState = ReadState.Aborted;
                _readErrorCode = evt.Data.PeerSendAbort.ErrorCode;
            }

            if (shouldComplete)
            {
                _receiveResettableCompletionSource.CompleteException(new QuicStreamAbortedException(_readErrorCode));
            }

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventPeerSendShutdown()
        {
            bool shouldComplete = false;

            lock (_sync)
            {
                // This event won't occur within the middle of a receive.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info("Completing resettable event source.");

                if (_readState == ReadState.None)
                {
                    shouldComplete = true;
                }

                _readState = ReadState.ReadsCompleted;
            }

            if (shouldComplete)
            {
                _receiveResettableCompletionSource.Complete(0);
            }

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventSendComplete(ref StreamEvent evt)
        {
            CleanupSendState();

            // TODO throw if a write was canceled.

            bool shouldComplete = false;
            lock (_sync)
            {
                if (_sendState == SendState.None)
                {
                    _sendState = SendState.Finished;
                    shouldComplete = true;
                }
            }

            if (shouldComplete)
            {
                _sendResettableCompletionSource.Complete(MsQuicStatusCodes.Success);
            }

            return MsQuicStatusCodes.Success;
        }

        private void CleanupSendState()
        {
            if (_sendHandle.IsAllocated)
            {
                _sendHandle.Free();
            }

            // Callings dispose twice on a memory handle should be okay
            foreach (MemoryHandle buffer in _bufferArrays)
            {
                buffer.Dispose();
            }
        }

        private void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);

            MsQuicApi.Api.SetCallbackHandlerDelegate(
                _ptr,
                s_streamDelegate,
                GCHandle.ToIntPtr(_handle));
        }

        // TODO prevent overlapping sends or consider supporting it.
        private unsafe ValueTask SendReadOnlyMemoryAsync(
           ReadOnlyMemory<byte> buffer,
           QUIC_SEND_FLAG flags)
        {
            if (buffer.IsEmpty)
            {
                if ((flags & QUIC_SEND_FLAG.FIN) == QUIC_SEND_FLAG.FIN)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            MemoryHandle handle = buffer.Pin();
            _sendQuicBuffers[0].Length = (uint)buffer.Length;
            _sendQuicBuffers[0].Buffer = (byte*)handle.Pointer;

            _bufferArrays[0] = handle;

            _sendHandle = GCHandle.Alloc(_sendQuicBuffers, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(_sendQuicBuffers, 0);

            uint status = MsQuicApi.Api.StreamSendDelegate(
                _ptr,
                quicBufferPointer,
                bufferCount: 1,
                (uint)flags,
                _ptr);

            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                CleanupSendState();

                // TODO this may need to be an aborted exception.
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _sendResettableCompletionSource.GetTypelessValueTask();
        }

        private unsafe ValueTask SendReadOnlySequenceAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAG flags)
        {
            if (buffers.IsEmpty)
            {
                if ((flags & QUIC_SEND_FLAG.FIN) == QUIC_SEND_FLAG.FIN)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            uint count = 0;

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                ++count;
            }

            if (_sendQuicBuffers.Length < count)
            {
                _sendQuicBuffers = new QuicBuffer[count];
                _bufferArrays = new MemoryHandle[count];
            }

            count = 0;

            foreach (ReadOnlyMemory<byte> buffer in buffers)
            {
                MemoryHandle handle = buffer.Pin();
                _sendQuicBuffers[count].Length = (uint)buffer.Length;
                _sendQuicBuffers[count].Buffer = (byte*)handle.Pointer;
                _bufferArrays[count] = handle;
                ++count;
            }

            _sendHandle = GCHandle.Alloc(_sendQuicBuffers, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(_sendQuicBuffers, 0);

            uint status = MsQuicApi.Api.StreamSendDelegate(
                _ptr,
                quicBufferPointer,
                count,
                (uint)flags,
                _ptr);

            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                CleanupSendState();

                // TODO this may need to be an aborted exception.
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _sendResettableCompletionSource.GetTypelessValueTask();
        }

        private unsafe ValueTask SendReadOnlyMemoryListAsync(
           ReadOnlyMemory<ReadOnlyMemory<byte>> buffers,
           QUIC_SEND_FLAG flags)
        {
            if (buffers.IsEmpty)
            {
                if ((flags & QUIC_SEND_FLAG.FIN) == QUIC_SEND_FLAG.FIN)
                {
                    // Start graceful shutdown sequence if passed in the fin flag and there is an empty buffer.
                    MsQuicApi.Api.StreamShutdownDelegate(_ptr, (uint)QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, errorCode: 0);
                }
                return default;
            }

            ReadOnlyMemory<byte>[] array = buffers.ToArray();

            uint length = (uint)array.Length;

            if (_sendQuicBuffers.Length < length)
            {
                _sendQuicBuffers = new QuicBuffer[length];
                _bufferArrays = new MemoryHandle[length];
            }

            for (int i = 0; i < length; i++)
            {
                ReadOnlyMemory<byte> buffer = array[i];
                MemoryHandle handle = buffer.Pin();
                _sendQuicBuffers[i].Length = (uint)buffer.Length;
                _sendQuicBuffers[i].Buffer = (byte*)handle.Pointer;
                _bufferArrays[i] = handle;
            }

            _sendHandle = GCHandle.Alloc(_sendQuicBuffers, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(_sendQuicBuffers, 0);

            uint status = MsQuicApi.Api.StreamSendDelegate(
                _ptr,
                quicBufferPointer,
                length,
                (uint)flags,
                _ptr);

            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                CleanupSendState();

                // TODO this may need to be an aborted exception.
                QuicExceptionHelpers.ThrowIfFailed(status,
                    "Could not send data to peer.");
            }

            return _sendResettableCompletionSource.GetTypelessValueTask();
        }

        /// <summary>
        /// Assigns a stream ID and begins process the stream.
        /// </summary>
        private void StartLocalStream()
        {
            Debug.Assert(!_started);
            uint status = MsQuicApi.Api.StreamStartDelegate(
              _ptr,
              (uint)QUIC_STREAM_START_FLAG.ASYNC);

            QuicExceptionHelpers.ThrowIfFailed(status, "Could not start stream.");
        }

        private void ReceiveComplete(int bufferLength)
        {
            uint status = MsQuicApi.Api.StreamReceiveCompleteDelegate(_ptr, (ulong)bufferLength);
            QuicExceptionHelpers.ThrowIfFailed(status, "Could not complete receive call.");
        }

        // This can fail if the stream isn't started.
        private unsafe long GetStreamId()
        {
            return (long)MsQuicParameterHelpers.GetULongParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.STREAM, (uint)QUIC_PARAM_STREAM.ID);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MsQuicStream));
            }
        }

        private enum ReadState
        {
            None,
            IndividualReadComplete,
            ReadsCompleted,
            Aborted
        }

        private enum ShutdownWriteState
        {
            None,
            Canceled,
            Finished
        }

        private enum SendState
        {
            None,
            Aborted,
            Finished
        }
    }
}
