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
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicStream : TransportConnection
    {
        private Task _processingTask;
        private MsQuicConnection _connection;
        private readonly CancellationTokenSource _streamClosedTokenSource = new CancellationTokenSource();
        private IMsQuicTrace _trace;
        private bool _disposed;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;
        private StreamCallback _callback;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;

        public MsQuicStream(MsQuicApi registration, MsQuicConnection connection, MsQuicTransportContext context, QUIC_STREAM_OPEN_FLAG flags, IntPtr nativeObjPtr)
        {
            Debug.Assert(connection != null);

            Registration = registration;
            _nativeObjPtr = nativeObjPtr;
            StreamCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);

            _connection = connection;
            MemoryPool = context.Options.MemoryPoolFactory();
            _trace = context.Log;

            ConnectionClosed = _streamClosedTokenSource.Token;

            var maxReadBufferSize = context.Options.MaxReadBufferSize.Value;
            var maxWriteBufferSize = context.Options.MaxWriteBufferSize.Value;

            // TODO should we allow these PipeScheduler to be configurable here?
            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            if (flags.HasFlag(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL))
            {
                Features.Set<IUnidirectionalStreamFeature>(new UnidirectionalStreamFeature());
            }

            // TODO populate the ITlsConnectionFeature (requires client certs). 
            var feature = new FakeTlsConnectionFeature();
            Features.Set<ITlsConnectionFeature>(feature);

            Transport = pair.Transport;
            Application = pair.Application;

            SetCallbackHandler(HandleStreamEvent);
            _processingTask = ProcessSends();
        }

        public override MemoryPool<byte> MemoryPool { get; }

        private async Task ProcessSends()
        {
            var output = Output;
            try
            {
                while (true)
                {
                    var result = await output.ReadAsync();

                    if (result.IsCanceled)
                    {
                        ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 0);
                        break;
                    }

                    var buffer = result.Buffer;

                    var end = buffer.End;
                    var isCompleted = result.IsCompleted;
                    if (!buffer.IsEmpty)
                    {
                        // Invalid parameter here?
                        await SendAsync(buffer, QUIC_SEND_FLAG.NONE, null);
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
                // TODO log
            }
        }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public uint HandleStreamEvent(
            ref MsQuicNativeMethods.StreamEvent evt)
        {
            var status = MsQuicConstants.Success;

            switch (evt.Type)
            {
                case QUIC_STREAM_EVENT.START_COMPLETE:
                    status = HandleStartComplete();
                    break;
                case QUIC_STREAM_EVENT.RECV:
                    {
                        status = HandleEventRecv(
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
            return MsQuicConstants.Success;
        }

        private uint HandleEventSendShutdownComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            return MsQuicConstants.Success;
        }

        private uint HandleEventPeerSendClose()
        {
            // TODO complete async
            Input.Complete();
            return MsQuicConstants.Success;
        }

        public uint HandleEventSendComplete(ref MsQuicNativeMethods.StreamEvent evt)
        {
            return MsQuicConstants.Success;
        }

        protected uint HandleEventRecv(ref MsQuicNativeMethods.StreamEvent evt)
        {
            var input = Input;
            var length = (int)evt.TotalBufferLength;
            var result = input.GetSpan(length);
            evt.CopyToBuffer(result);
            input.Advance(length);

            var flushTask = input.FlushAsync();

            if (!flushTask.IsCompletedSuccessfully)
            {
                _ = AwaitFlush(flushTask);

                return MsQuicConstants.Pending;
            }

            async Task AwaitFlush(ValueTask<FlushResult> ft)
            {
                await ft;
                EnableReceive();
            }

            return MsQuicConstants.Success;
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            Shutdown(abortReason);

            // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
            Output.CancelPendingRead();
        }

        private void Shutdown(Exception shutdownReason)
        {
            // TODO move stream shutudown logic here.
        }

        public delegate uint StreamCallback(
            ref StreamEvent evt);

        public MsQuicApi Registration { get; set; }

        internal StreamCallbackDelegate NativeCallback { get; private set; }

        internal static uint NativeCallbackHandler(
           IntPtr stream,
           IntPtr context,
           ref StreamEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicStream = (MsQuicStream)handle.Target;

            return quicStream.ExecuteCallback(ref connectionEventStruct);
        }

        private uint ExecuteCallback(
            ref StreamEvent evt)
        {
            var status = MsQuicConstants.InternalError;
            if (evt.Type == QUIC_STREAM_EVENT.SEND_COMPLETE)
            {
                SendContext.HandleNativeCallback(evt.ClientContext);
            }
            try
            {
                status = _callback(
                    ref evt);
            }
            catch (Exception)
            {
                // TODO log
            }
            return status;
        }

        public void SetCallbackHandler(
            StreamCallback callback)
        {
            _handle = GCHandle.Alloc(this);
            _callback = callback;
            Registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
                GCHandle.ToIntPtr(_handle));
        }

        public unsafe Task SendAsync(
           ReadOnlySequence<byte> buffers,
           QUIC_SEND_FLAG flags,
           object clientSendContext)
        {
            var sendContext = new SendContext(buffers, clientSendContext);
            var status = Registration.StreamSendDelegate(
                _nativeObjPtr,
                sendContext.Buffers,
                sendContext.BufferCount,
                (uint)flags,
                sendContext.NativeClientSendContext);
            MsQuicStatusException.ThrowIfFailed(status);

            return sendContext.TcsTask; // TODO make QuicStream implement IValueTaskSource?
        }

        public Task StartAsync(QUIC_STREAM_START_FLAG flags)
        {
            var status = (uint)Registration.StreamStartDelegate(
              _nativeObjPtr,
              (uint)flags);
            MsQuicStatusException.ThrowIfFailed(status);
            return Task.CompletedTask;
        }

        public void ReceiveComplete(int bufferLength)
        {
            var status = (uint)Registration.StreamReceiveComplete(_nativeObjPtr, (ulong)bufferLength);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public void ShutDown(
            QUIC_STREAM_SHUTDOWN_FLAG flags,
            ushort errorCode)
        {
            var status = (uint)Registration.StreamShutdownDelegate(
                _nativeObjPtr,
                (uint)flags,
                errorCode);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public void Close()
        {
            var status = (uint)Registration.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            MsQuicStatusException.ThrowIfFailed(status);
        }

        public long Handle { get => (long)_nativeObjPtr; }

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

        private void SetParam(
              QUIC_PARAM_STREAM param,
              QuicBuffer buf)
        {
            MsQuicStatusException.ThrowIfFailed(Registration.UnsafeSetParam(
                _nativeObjPtr,
                (uint)QUIC_PARAM_LEVEL.SESSION,
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
                Registration.StreamCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            Registration = null;

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
