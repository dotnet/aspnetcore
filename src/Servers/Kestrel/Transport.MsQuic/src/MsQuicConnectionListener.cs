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
using static Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal.MsQuicNativeMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic
{
    /// <summary>
    /// Listens for new Quic Connections.
    /// </summary>
    public class MsQuicConnectionListener : IConnectionListener, IAsyncDisposable, IDisposable
    {
        private MsQuicApi _registration;
        private QuicSecConfig _secConfig;
        private QuicSession _session;
        private bool _disposed;
        private bool _stopped;
        private IntPtr _nativeObjPtr;
        private readonly IntPtr _unmanagedFnPtrForNativeCallback;
        private GCHandle _handle;

        private readonly Channel<MsQuicConnection> _acceptConnectionQueue = Channel.CreateUnbounded<MsQuicConnection>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        public MsQuicConnectionListener(MsQuicTransportContext transportContext, EndPoint endpoint)
        {
            _registration = new MsQuicApi();
            TransportContext = transportContext;
            EndPoint = endpoint;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate((ListenerCallbackDelegate)NativeCallbackHandler);
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

            _stopped = true;

            await DisposeAsync();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _registration.RegistrationOpen(Encoding.ASCII.GetBytes(TransportContext.Options.RegistrationName));

            _secConfig = await _registration.CreateSecurityConfig(TransportContext.Options.Certificate);

            _session = _registration.SessionOpen(TransportContext.Options.Alpn);

            _nativeObjPtr = _session.ListenerOpen(NativeCallbackHandler);

            SetCallbackHandler();

            _session.SetIdleTimeout(TransportContext.Options.IdleTimeout);
            _session.SetPeerBiDirectionalStreamCount(TransportContext.Options.MaxBidirectionalStreamCount);
            _session.SetPeerUnidirectionalStreamCount(TransportContext.Options.MaxBidirectionalStreamCount);

            Start();
        }

        internal uint ListenerCallbackHandler(
            ref ListenerEvent evt)
        {
            switch (evt.Type)
            {
                case QUIC_LISTENER_EVENT.NEW_CONNECTION:
                    {
                        evt.Data.NewConnection.SecurityConfig = _secConfig.NativeObjPtr;
                        var msQuicConnection = new MsQuicConnection(_registration, TransportContext, evt.Data.NewConnection.Connection);
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

        private void Start()
        {
            var address = MsQuicNativeMethods.Convert(EndPoint as IPEndPoint);
            MsQuicStatusException.ThrowIfFailed(_registration.ListenerStartDelegate(
                _nativeObjPtr,
                ref address));
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
            _registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
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

            if (_nativeObjPtr != IntPtr.Zero)
            {
                _registration.ListenerStopDelegate(_nativeObjPtr);
                _registration.ListenerCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _registration = null;
            _disposed = true;

            StopAcceptingConnections();

            return new ValueTask();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_nativeObjPtr != IntPtr.Zero)
            {
                _registration.ListenerStopDelegate(_nativeObjPtr);
                _registration.ListenerCloseDelegate(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _registration = null;
            _disposed = true;

            StopAcceptingConnections();
        }
    }
}
