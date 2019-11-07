// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic
{
    /// <summary>
    /// Listens for new Quic Connections.
    /// </summary>
    public class MsQuicConnectionListener : IConnectionListener, IAsyncDisposable, IDisposable
    {
        private IMsQuicTrace _log;
        private MsQuicApi _api;
        private QuicSecConfig _secConfig;
        private QuicSession _session;
        private bool _disposed;
        private bool _stopped;
        private IntPtr _nativeObjPtr;
        private GCHandle _handle;

        private readonly Channel<MsQuicConnection> _acceptConnectionQueue = Channel.CreateUnbounded<MsQuicConnection>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public MsQuicConnectionListener(MsQuicTransportContext transportContext, EndPoint endpoint)
        {
            _log = transportContext.Log;
            _api = new MsQuicApi();
            TransportContext = transportContext;
            EndPoint = endpoint;
        }

        public MsQuicTransportContext TransportContext { get; }
        public EndPoint EndPoint { get; set; }
        public IHostApplicationLifetime AppLifetime => TransportContext.AppLifetime;
        public IMsQuicTrace Log => TransportContext.Log;

        public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (await _acceptConnectionQueue.Reader.WaitToReadAsync())
            {
                if (_acceptConnectionQueue.Reader.TryRead(out var connection))
                {
                    return connection;
                }
            }

            return null;
        }

        internal async Task BindAsync()
        {
            await StartAsync();
        }

        public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            if (_stopped)
            {
                return;
            }

            // TODO abort all streams and connections here?
            _stopped = true;

            await DisposeAsync();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _api.RegistrationOpen(Encoding.ASCII.GetBytes(TransportContext.Options.RegistrationName));

            _secConfig = await _api.CreateSecurityConfig(TransportContext.Options.Certificate);

            _session = _api.SessionOpen(TransportContext.Options.Alpn);
            _log.LogDebug(0, "Started session");

            _nativeObjPtr = _session.ListenerOpen(NativeCallbackHandler);

            SetCallbackHandler();

            _session.SetIdleTimeout(TransportContext.Options.IdleTimeout);
            _session.SetPeerBiDirectionalStreamCount(TransportContext.Options.MaxBidirectionalStreamCount);
            _session.SetPeerUnidirectionalStreamCount(TransportContext.Options.MaxBidirectionalStreamCount);

            var address = MsQuicNativeMethods.Convert(EndPoint as IPEndPoint);
            MsQuicStatusException.ThrowIfFailed(_api.ListenerStartDelegate(
                _nativeObjPtr,
                ref address));
        }

        internal uint ListenerCallbackHandler(
            ref ListenerEvent evt)
        {
            switch (evt.Type)
            {
                case QUIC_LISTENER_EVENT.NEW_CONNECTION:
                    {
                        evt.Data.NewConnection.SecurityConfig = _secConfig.NativeObjPtr;
                        var msQuicConnection = new MsQuicConnection(_api, TransportContext, evt.Data.NewConnection.Connection);
                        _acceptConnectionQueue.Writer.TryWrite(msQuicConnection);
                    }
                    break;
                default:
                    return MsQuicConstants.InternalError;
            }

            return MsQuicConstants.Success;
        }

        protected void StopAcceptingConnections()
        {
            _acceptConnectionQueue.Writer.TryComplete();
        }

        internal static uint NativeCallbackHandler(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent connectionEventStruct)
        {
            var handle = GCHandle.FromIntPtr(context);
            var quicListener = (MsQuicConnectionListener)handle.Target;

            return quicListener.ListenerCallbackHandler(ref connectionEventStruct);
        }

        internal void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);
            _api.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                new ListenerCallbackDelegate(NativeCallbackHandler),
                GCHandle.ToIntPtr(_handle));
        }

        ~MsQuicConnectionListener()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return new ValueTask();
            }

            Dispose(true);

            return new ValueTask();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            StopAcceptingConnections();

            if (_nativeObjPtr != IntPtr.Zero)
            {
                _api.ListenerStopDelegate(_nativeObjPtr);
                _api.ListenerCloseDelegate(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _api = null;
            _disposed = true;
        }
    }
}
