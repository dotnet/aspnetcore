// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicConnection : TransportConnection, IStreamListener
    {
        private QuicConnection _connection;
        private IAsyncEnumerator<MsQuicStream> _acceptEnumerator;
        private readonly Channel<MsQuicStream> _acceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public MsQuicConnection(QuicConnection connection)
        {
            _connection = connection;
            connection.SetCallbackHandler(HandleEvent);
            connection.SetIdleTimeout(TimeSpan.FromMinutes(10));

            _acceptEnumerator = AcceptStreamsAsync();
            Features.Set<IStreamListener>(this);
        }

        private async IAsyncEnumerator<MsQuicStream> AcceptStreamsAsync()
        {
            while (true)
            {
                while (await _acceptQueue.Reader.WaitToReadAsync())
                {
                    while (_acceptQueue.Reader.TryRead(out var stream))
                    {
                        yield return stream;
                    }
                }

                yield return null;
            }
        }

        public QUIC_STATUS HandleEvent(
            QuicConnection connection,
            ref NativeMethods.ConnectionEvent evt)
        {
            var status = HandleEventCore(connection, ref evt);

            return status;
        }

        private QUIC_STATUS HandleEventCore(
          QuicConnection connection,
          ref NativeMethods.ConnectionEvent connectionEvent)
        {
            var status = QUIC_STATUS.SUCCESS;
            switch (connectionEvent.Type)
            {
                case QUIC_CONNECTION_EVENT.CONNECTED:
                    {
                        status = HandleEventConnected(
                            connection,
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.SHUTDOWN_BEGIN:
                    {
                        status = HandleEventShutdownBegin(
                            connection,
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.SHUTDOWN_BEGIN_PEER:
                    {
                        status = HandleEventShutdownBeginPeer(
                            connection,
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.SHUTDOWN_COMPLETE:
                    {
                        status = HandleEventShutdownComplete(
                            connection,
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.LOCAL_ADDR_CHANGED:
                    {
                        status = HandleEventLocalAddrChanged(
                            connection,
                            connectionEvent);
                    }
                    break;


                case QUIC_CONNECTION_EVENT.PEER_ADDR_CHANGED:
                    {
                        status = HandleEventPeerAddrChanged(
                            connection,
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.NEW_STREAM:
                    {
                        status = HandleEventNewStream(
                            connection,
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE:
                    {
                        status = HandleEventStreamsAvailable(
                            connection,
                            connectionEvent);
                    }
                    break;

                case QUIC_CONNECTION_EVENT.PEER_NEEDS_STREAMS:
                    {
                    }
                    break;

                case QUIC_CONNECTION_EVENT.IDEAL_SEND_BUFFER:
                    {
                        status = HandleEventIdealSendBuffer(
                            connection,
                            connectionEvent);
                    }
                    break;

                default:
                    break;
            }
            return status;
        }

        protected virtual QUIC_STATUS HandleEventConnected(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventShutdownBegin(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventShutdownBeginPeer(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventShutdownComplete(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventLocalAddrChanged(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventPeerAddrChanged(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventNewStream(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            var stream = connectionEvent.CreateNewStream(connection.Registration);
            var streamWrapper = new MsQuicStream(this, connectionEvent.StreamFlags);

            streamWrapper.Start(stream);
            _acceptQueue.Writer.TryWrite(streamWrapper);

            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventStreamsAvailable(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        protected virtual QUIC_STATUS HandleEventIdealSendBuffer(
            QuicConnection connection,
            NativeMethods.ConnectionEvent connectionEvent)
        {
            return QUIC_STATUS.SUCCESS;
        }

        public async Task<ConnectionContext> StartUnidirectionalStreamAsync()
        {
            var flags = QUIC_NEW_STREAM_FLAG.UNIDIRECTIONAL;
            var msquicStream = new MsQuicStream(this, flags);
            var stream = _connection.StreamOpen(flags, msquicStream.HandleStreamEvent);
            await stream.StartAsync(QUIC_STREAM_START.NONE);
            msquicStream.Start(stream);
            return msquicStream;
        }

        public async Task<ConnectionContext> AcceptAsync()
        {
            if (await _acceptEnumerator.MoveNextAsync())
            {
                return _acceptEnumerator.Current;
            }

            return null;
        }
    }
}
