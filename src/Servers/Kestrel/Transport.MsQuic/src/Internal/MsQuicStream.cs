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
    internal class MsQuicStream : TransportConnection, IUnidirectionalStreamFeature
    {
        private Task _processingTask;
        private MsQuicConnection _connection;
        private readonly CancellationTokenSource _streamClosedTokenSource = new CancellationTokenSource();
        private IMsQuicTrace _trace;
        private bool _disposed;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;

        internal ResettableCompletionSource _resettableCts;
        private MemoryHandle[] _bufferArrays;
        private GCHandle _sendBuffer;

        public MsQuicStream(MsQuicApi registration, MsQuicConnection connection, MsQuicTransportContext context, QUIC_STREAM_OPEN_FLAG flags, IntPtr nativeObjPtr)
        {
            Debug.Assert(connection != null);

            Registration = registration;
            _nativeObjPtr = nativeObjPtr;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate((StreamCallbackDelegate)NativeCallbackHandler);

            _connection = connection;
            MemoryPool = context.Options.MemoryPoolFactory();
            _trace = context.Log;

            ConnectionClosed = _streamClosedTokenSource.Token;

            var maxReadBufferSize = context.Options.MaxReadBufferSize.Value;
            var maxWriteBufferSize = context.Options.MaxWriteBufferSize.Value;
            _resettableCts = new ResettableCompletionSource(this);

            // TODO should we allow these PipeScheduler to be configurable here?
            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            if (flags.HasFlag(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL))
            {
                Features.Set<IUnidirectionalStreamFeature>(this);
            }

            // TODO populate the ITlsConnectionFeature (requires client certs). 
            var feature = new FakeTlsConnectionFeature();
            Features.Set<ITlsConnectionFeature>(feature);

            Transport = pair.Transport;
            Application = pair.Application;

            SetCallbackHandler();

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
                        // TODO how to get abort codepath sync'd
                        ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 0);
                        break;
                    }

                    var buffer = result.Buffer;

                    var end = buffer.End;
                    var isCompleted = result.IsCompleted;
                    if (!buffer.IsEmpty)
                    {
                        // Invalid parameter here?
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

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

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
            _resettableCts.Complete(MsQuicConstants.Success);
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
            _sendBuffer.Free();
            foreach (var gchBufferArray in _bufferArrays)
            {
                gchBufferArray.Dispose();
            }
            _resettableCts.Complete(evt.SendAbortError);
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

        public MsQuicApi Registration { get; set; }

        internal StreamCallbackDelegate NativeCallback { get; private set; }

        internal static uint NativeCallbackHandler(
           IntPtr stream,
           IntPtr context,
           ref StreamEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicStream = (MsQuicStream)handle.Target;

            return quicStream.HandleEvent(ref connectionEventStruct);
        }

        public void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);
            Registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
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

            // I don't know if I need to pin memory here.
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

            var status = Registration.StreamSendDelegate(
                _nativeObjPtr,
                quicBufferPointer,
                (uint)bufferCount,
                (uint)flags,
                _nativeObjPtr);

            MsQuicStatusException.ThrowIfFailed(status);

            return _resettableCts.GetValueTask();
        }

        public ValueTask<uint> StartAsync()
        {
            // TODO make start actually async here.
            var status = Registration.StreamStartDelegate(
              _nativeObjPtr,
              (uint)QUIC_STREAM_START_FLAG.ASYNC);

            MsQuicStatusException.ThrowIfFailed(status);
            return _resettableCts.GetValueTask();
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
