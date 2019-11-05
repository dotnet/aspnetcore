// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicStream : TransportConnection
    {
        private Task _processingTask;
        private QuicStream _stream;
        private MsQuicConnection _connection;
        private readonly CancellationTokenSource _streamClosedTokenSource = new CancellationTokenSource();
        private IMsQuicTrace _trace;

        public MsQuicStream(MsQuicConnection connection, MemoryPool<byte> memoryPool, PipeScheduler scheduler, IMsQuicTrace trace, QUIC_STREAM_OPEN_FLAG flags, long? maxReadBufferSize = null, long? maxWriteBufferSize = null)
        {
            Debug.Assert(connection != null);
            Debug.Assert(memoryPool != null);
            Debug.Assert(trace != null);

            _connection = connection;
            MemoryPool = memoryPool;
            _trace = trace;

            ConnectionClosed = _streamClosedTokenSource.Token;

            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, scheduler, maxReadBufferSize.Value, maxReadBufferSize.Value / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, scheduler, PipeScheduler.ThreadPool, maxWriteBufferSize.Value, maxWriteBufferSize.Value / 2, useSynchronizationContext: false);

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
        }

        public override MemoryPool<byte> MemoryPool { get; }

        // TODO make start async now that we have an event.
        public void Start(QuicStream stream)
        {
            _stream = stream;
            stream.SetCallbackHandler(HandleStreamEvent);
            _processingTask = ProcessSends();
        }

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
                        _stream.ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 0);
                        break;
                    }

                    var buffer = result.Buffer;

                    var end = buffer.End;
                    var isCompleted = result.IsCompleted;
                    if (!buffer.IsEmpty)
                    {
                        // Invalid parameter here?
                        await _stream.SendAsync(buffer, QUIC_SEND_FLAG.NONE, null);
                    }

                    output.AdvanceTo(end);

                    if (isCompleted)
                    {
                        // Once the stream pipe is closed, shutdown the stream.
                        _stream.ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.GRACEFUL, 0);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                _stream.ShutDown(QUIC_STREAM_SHUTDOWN_FLAG.ABORT, 0);
                // TODO log
            }
        }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public uint HandleStreamEvent(
            QuicStream stream,
            ref MsQuicNativeMethods.StreamEvent evt)
        {
            var status = MsQuicConstants.Success;

            switch (evt.Type)
            {
                case QUIC_STREAM_EVENT.START_COMPLETE:
                    status = HandleStartComplete(stream);
                    break;
                case QUIC_STREAM_EVENT.RECV:
                    {
                        status = HandleEventRecv(
                            stream,
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
                        _stream.Close();
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

        private uint HandleStartComplete(QuicStream stream)
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

        protected uint HandleEventRecv(QuicStream stream, ref MsQuicNativeMethods.StreamEvent evt)
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
                stream.EnableReceive();
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
