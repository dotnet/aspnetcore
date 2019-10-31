// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicStream : TransportConnection
    {
        private Task _processingTask;
        private QuicStream _stream;
        private MsQuicConnection _connection;
        private readonly CancellationTokenSource _streamClosedTokenSource = new CancellationTokenSource();

        public MsQuicStream(MsQuicConnection connection, QUIC_NEW_STREAM_FLAG flags)
        {
            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, 1024 * 1024, 1024 * 1024 / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, 1024 * 64, 1024 * 64 / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);
            if (flags.HasFlag(QUIC_NEW_STREAM_FLAG.UNIDIRECTIONAL))
            {
                Features.Set<IUnidirectionalStreamFeature>(new UnidirectionalStreamFeature());
            }

            // Add a fake TLS connection feature becuase quic is always secure
            var feature = new FakeTlsConnectionFeature();
            Features.Set<ITlsConnectionFeature>(feature);

            // Set the transport and connection id
            Transport = pair.Transport;
            Application = pair.Application;
            _connection = connection;
            ConnectionClosed = _streamClosedTokenSource.Token;
        }

        public MsQuicStream(QuicStream stream, bool _)
        {
            var inputOptions = new PipeOptions(MemoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, 1024 * 1024, 1024 * 1024 / 2, useSynchronizationContext: false);
            var outputOptions = new PipeOptions(MemoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, 1024 * 64, 1024 * 64 / 2, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            // Set the transport and connection id
            Transport = pair.Transport;
            Application = pair.Application;
            _stream = stream;
        }

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
                        _stream.ShutDown(QUIC_STREAM_SHUTDOWN.GRACEFUL, 0);
                        break;
                    }
                }
            }
            catch (Exception)
            {
                _stream.ShutDown(QUIC_STREAM_SHUTDOWN.ABORT, 0);
                // TODO log
            }
        }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public QUIC_STATUS HandleStreamEvent(
            QuicStream stream,
            ref NativeMethods.StreamEvent evt)
        {
            var status = QUIC_STATUS.SUCCESS;

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
                        return QUIC_STATUS.SUCCESS;
                    }

                default:
                    break;
            }
            return status;
        }

        private QUIC_STATUS HandleEventPeerRecvAbort()
        {
            return QUIC_STATUS.SUCCESS;
        }

        private QUIC_STATUS HandleEventPeerSendAbort()
        {
            return QUIC_STATUS.SUCCESS;
        }

        private QUIC_STATUS HandleStartComplete(QuicStream stream)
        {
            return QUIC_STATUS.SUCCESS;
        }

        private QUIC_STATUS HandleEventSendShutdownComplete(ref NativeMethods.StreamEvent evt)
        {
            return QUIC_STATUS.SUCCESS;
        }

        private QUIC_STATUS HandleEventPeerSendClose()
        {
            // TODO complete async
            // Close as fast as possible here.
            Input.Complete();
            return QUIC_STATUS.SUCCESS;
        }

        public QUIC_STATUS HandleEventSendComplete(ref NativeMethods.StreamEvent evt)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected QUIC_STATUS HandleEventRecv(QuicStream stream, ref NativeMethods.StreamEvent evt)
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

                return QUIC_STATUS.PENDING;
            }

            async Task AwaitFlush(ValueTask<FlushResult> ft)
            {
                await ft;
                stream.EnableReceive();
            }

            return QUIC_STATUS.SUCCESS;
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
