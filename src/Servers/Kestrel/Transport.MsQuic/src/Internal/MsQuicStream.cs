// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicStream : TransportConnection, IQuicStreamFeature
    {
        private Task _processingTask;
        private MsQuicConnection _connection;
        private readonly CancellationTokenSource _streamClosedTokenSource = new CancellationTokenSource();
        private IMsQuicTrace _log;
        private bool _disposed;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;
        private StreamCallbackDelegate _delegate;
        private string _connectionId;
        private long _streamId = -1;

        internal ResettableCompletionSource _resettableCompletion;
        private MemoryHandle[] _bufferArrays;
        private GCHandle _sendBuffer;

        public MsQuicStream(MsQuicApi api, MsQuicConnection connection, MsQuicTransportContext context, QUIC_STREAM_OPEN_FLAG flags, IntPtr nativeObjPtr)
        {
            Debug.Assert(connection != null);

            Api = api;
            _nativeObjPtr = nativeObjPtr;

            _connection = connection;
            MemoryPool = context.Options.MemoryPoolFactory();
            _log = context.Log;

            ConnectionClosed = _streamClosedTokenSource.Token;

            var maxReadBufferSize = context.Options.MaxReadBufferSize.Value;
            var maxWriteBufferSize = context.Options.MaxWriteBufferSize.Value;
            _resettableCompletion = new ResettableCompletionSource(this);

            // TODO should we allow these PipeScheduler to be configurable here?
            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            Features.Set<IQuicStreamFeature>(this);

            // TODO populate the ITlsConnectionFeature (requires client certs). 
            var feature = new FakeTlsConnectionFeature();
            Features.Set<ITlsConnectionFeature>(feature);
            if (flags.HasFlag(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL))
            {
                IsUnidirectional = true;
            }

            Transport = pair.Transport;
            Application = pair.Application;

            SetCallbackHandler();

            _processingTask = ProcessSends();
        }

        public override MemoryPool<byte> MemoryPool { get; }
        public PipeWriter Input => Application.Output;
        public PipeReader Output => Application.Input;
        public bool IsUnidirectional { get; }
        public long StreamId
        {
            get
            {
                if (_streamId == -1)
                {
                    _streamId = GetStreamId();
                }

                return _streamId;
            }
        }

        public override string ConnectionId {
            get
            {
                if (_connectionId == null)
                {
                    _connectionId = $"{_connection.ConnectionId}:{StreamId}";
                }
                return _connectionId;
            }
            set
            {
                _connectionId = value;
            }
        }

        private async Task ProcessSends()
        {
            var output = Output;
            try
            {
                while (true)
                {
                    var result = await output.ReadAsync();
                    _log.LogDebug(0, "Handling send event");

                    if (result.IsCanceled)
                    {
                        // TODO how to get abort codepath sync'd
                        ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 0);
                        break;
                    }

                    var buffer = result.Buffer;

                    var end = buffer.End;
                    var isCompleted = result.IsCompleted;
                    if (!buffer.IsEmpty)
                    {
                        await SendAsync(buffer, QUIC_SEND_FLAG.NONE);
                    }

                    output.AdvanceTo(end);

                    if (isCompleted)
                    {
                        // Once the stream pipe is closed, shutdown the stream.
                        ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, 0);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 0);
            }
        }

        internal uint HandleEvent(ref MsQuicNativeMethods.StreamEvent evt)
        {
            var status = MsQuicConstants.Success;

            switch (evt.Type)
            {
                case QUIC_STREAM_EVENT.START_COMPLETE:
                    status = HandleStartComplete();
                    break;
                case QUIC_STREAM_EVENT.RECV:
                    {
                        HandleEventRecv(
                            ref evt);
                    }
                    break;
                case QUIC_STREAM_EVENT.SEND_COMPLETE:
                    {
                        status = HandleEventSendComplete(ref evt);
                    }
                    break;
                case QUIC_STREAM_EVENT.PEER_SEND_CLOSE:
                    {
                        status = HandleEventPeerSendClose();
                    }
                    break;
                // TODO figure out difference between SEND_ABORT and RECEIVE_ABORT
                case QUIC_STREAM_EVENT.PEER_SEND_ABORT:
                    {
                        _streamClosedTokenSource.Cancel();
                        status = HandleEventPeerSendAbort();
                    }
                    break;
                case QUIC_STREAM_EVENT.PEER_RECV_ABORT:
                    {
                        _streamClosedTokenSource.Cancel();
                        status = HandleEventPeerRecvAbort();
                    }
                    break;
                case QUIC_STREAM_EVENT.SEND_SHUTDOWN_COMPLETE:
                    {
                        status = HandleEventSendShutdownComplete(ref evt);
                    }
                    break;
                case QUIC_STREAM_EVENT.SHUTDOWN_COMPLETE:
                    {
                        Close();
                        return MsQuicConstants.Success;
                    }

                default:
                    break;
            }
            return status;
        }

        private uint HandleEventPeerRecvAbort()
        {
            return MsQuicConstants.Success;
        }

        private uint HandleEventPeerSendAbort()
        {
            return MsQuicConstants.Success;
        }

        private uint HandleStartComplete()
        {
            _resettableCompletion.Complete(MsQuicConstants.Success);
            return MsQuicConstants.Success;
        }

        private uint HandleEventSendShutdownComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            return MsQuicConstants.Success;
        }

        private uint HandleEventPeerSendClose()
        {
            Input.Complete();
            return MsQuicConstants.Success;
        }

        public uint HandleEventSendComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            _sendBuffer.Free();
            foreach (var gchBufferArray in _bufferArrays)
            {
                gchBufferArray.Dispose();
            }
            _resettableCompletion.Complete(evt.Data.PeerRecvAbort.ErrorCode);
            return MsQuicConstants.Success;
        }

        protected void HandleEventRecv(ref MsQuicNativeMethods.StreamEvent evt)
        {
            static unsafe void CopyToBuffer(Span<byte> buffer, StreamEvent evt)
            {
                var length = (int)evt.Data.Recv.Buffers[0].Length;
                new Span<byte>(evt.Data.Recv.Buffers[0].Buffer, length).CopyTo(buffer);
            }

            _log.LogDebug(0, "Handling receive event");
            var input = Input;
            var length = (int)evt.Data.Recv.TotalBufferLength;
            var result = input.GetSpan(length);
            CopyToBuffer(result, evt);

            input.Advance(length);

            var flushTask = input.FlushAsync();

            if (!flushTask.IsCompletedSuccessfully)
            {
                _ = AwaitFlush(flushTask);

                return;
            }

            async Task AwaitFlush(ValueTask<FlushResult> ft)
            {
                await ft;
                // TODO figure out when to call these for receive.
                EnableReceive();
                ReceiveComplete(length);
            }
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            Shutdown(abortReason);

            // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
            Output.CancelPendingRead();
        }

        private void Shutdown(Exception shutdownReason)
        {
        }

        public MsQuicApi Api { get; set; }

        internal static uint NativeCallbackHandler(
           IntPtr stream,
           IntPtr context,
           StreamEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicStream = (MsQuicStream)handle.Target;

            return quicStream.HandleEvent(ref connectionEventStruct);
        }

        public void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);

            _delegate = new StreamCallbackDelegate(NativeCallbackHandler);
            Api.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _delegate,
                GCHandle.ToIntPtr(_handle));
        }

        public unsafe ValueTask<uint> SendAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAG flags)
        {
            var bufferCount = 0;
            foreach (var memory in buffers)
            {
                bufferCount++;
            }

            var quicBufferArray = new QuicBuffer[bufferCount];
            _bufferArrays = new MemoryHandle[bufferCount];

            var i = 0;
            foreach (var memory in buffers)
            {
                var handle = memory.Pin();
                _bufferArrays[i] = handle;
                quicBufferArray[i].Length = (uint)memory.Length;
                quicBufferArray[i].Buffer = (byte*)handle.Pointer;
                i++;
            }

            _sendBuffer = GCHandle.Alloc(quicBufferArray, GCHandleType.Pinned);

            var quicBufferPointer = (QuicBuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(quicBufferArray, 0);

            var status = Api.StreamSendDelegate(
                _nativeObjPtr,
                quicBufferPointer,
                (uint)bufferCount,
                (uint)flags,
                _nativeObjPtr);

            MsQuicStatusException.ThrowIfFailed(status);

            return _resettableCompletion.GetValueTask();
        }

        public ValueTask<uint> StartAsync()
        {
            var status = Api.StreamStartDelegate(
              _nativeObjPtr,
              (uint)QUIC_STREAM_START_FLAG.ASYNC);

            MsQuicStatusException.ThrowIfFailed(status);
            return _resettableCompletion.GetValueTask();
        }

        public void ReceiveComplete(int bufferLength)
        {
            var status = (uint)Api.StreamReceiveComplete(_nativeObjPtr, (ulong)bufferLength);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public void ShutDown(
            QUIC_STREAM_SHUTDOWN_FLAG flags,
            ushort errorCode)
        {
            var status = (uint)Api.StreamShutdownDelegate(
                _nativeObjPtr,
                (uint)flags,
                errorCode);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public void Close()
        {
            var status = (uint)Api.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public unsafe void EnableReceive()
        {
            var val = true;
            var buffer = new QuicBuffer()
            {
                Length = sizeof(bool),
                Buffer = (byte*)&val
            };
            SetParam(QUIC_PARAM_STREAM.RECEIVE_ENABLED, buffer);
        }

        private unsafe long GetStreamId()
        {
            var byteArr = new byte[sizeof(long)];
            fixed (byte* ptr = byteArr)
            {
                var buffer = new QuicBuffer()
                {
                    Length = (uint)byteArr.Length,
                    Buffer = ptr
                };
                GetParam(QUIC_PARAM_STREAM.ID, buffer);
                return BitConverter.ToInt64(byteArr);
            }
        }

        private void GetParam(
            QUIC_PARAM_STREAM param,
            QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(Api.UnsafeGetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.STREAM,
                (uint)param,
                ref buf));
        }

        private void SetParam(
              QUIC_PARAM_STREAM param,
              QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(Api.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.STREAM,
                (uint)param,
                buf));
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

            if (_nativeObjPtr != IntPtr.Zero)
            {
                Api.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _handle.Free();
            _nativeObjPtr = IntPtr.Zero;
            Api = null;

            _disposed = true;
        }
    }

    internal class FakeTlsConnectionFeature : ITlsConnectionFeature
    {
        public FakeTlsConnectionFeature()
        {
        }

        public X509Certificate2 ClientCertificate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
