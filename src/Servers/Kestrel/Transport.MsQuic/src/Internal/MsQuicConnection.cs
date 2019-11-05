// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions.Features;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicConnection : TransportConnection, IQuicStreamListenerFeature, IQuicCreateStreamFeature
    {
        private QuicConnection _connection;
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

            Features.Set<IQuicStreamListenerFeature>(this);
        }

        public uint HandleEvent(
            QuicConnection connection,
            ref MsQuicNativeMethods.ConnectionEvent evt)
        {
            var status = HandleEventCore(connection, ref evt);

            return status;
        }

        private uint HandleEventCore(
          QuicConnection connection,
          ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            var status = MsQuicConstants.Success;
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

        protected virtual uint HandleEventConnected(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownBegin(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownBeginPeer(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventShutdownComplete(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventLocalAddrChanged(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventPeerAddrChanged(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventNewStream(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            var stream = new QuicStream(connection.Registration, connectionEvent.Data.NewStream.Stream, shouldOwnNativeObj: false);
            var streamWrapper = new MsQuicStream(this, connectionEvent.StreamFlags);

            streamWrapper.Start(stream);
            _acceptQueue.Writer.TryWrite(streamWrapper);

            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventStreamsAvailable(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        protected virtual uint HandleEventIdealSendBuffer(
            QuicConnection connection,
            MsQuicNativeMethods.ConnectionEvent connectionEvent)
        {
            return MsQuicConstants.Success;
        }

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            while (await _acceptQueue.Reader.WaitToReadAsync())
            {
                while (_acceptQueue.Reader.TryRead(out var stream))
                {
                    return stream;
                }
            }

            return null;
        }

        public ValueTask<ConnectionContext> StartUnidirectionalStreamAsync()
        {
            return StartStreamAsync(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);
        }

        public ValueTask<ConnectionContext> StartBidirectionalStreamAsync()
        {
            return StartStreamAsync(QUIC_STREAM_OPEN_FLAG.NONE);
        }

        private async ValueTask<ConnectionContext> StartStreamAsync(QUIC_STREAM_OPEN_FLAG flags)
        {
            var msquicStream = new MsQuicStream(this, flags);
            var stream = _connection.StreamOpen(flags, msquicStream.HandleStreamEvent);
            await stream.StartAsync(QUIC_STREAM_START_FLAG.NONE);
            msquicStream.Start(stream);
            return msquicStream;
        }
    }
}
