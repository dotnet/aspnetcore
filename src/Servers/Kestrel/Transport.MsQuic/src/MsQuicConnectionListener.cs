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
    /// Listens for new Quic Connections
    /// </summary>
    public class MsQuicConnectionListener : IConnectionListener, IAsyncDisposable, IDisposable
    {
        private MsQuicApi _registration;
        private QuicSecConfig _secConfig;
        private QuicSession _session;
        private IAsyncEnumerator<MsQuicConnection> _acceptEnumerator;
        private bool _disposed;
        private bool _stopped;
        private IntPtr _nativeObjPtr;
        private ListenerCallback _callback;
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
            ListenerCallbackDelegate nativeCallback = NativeCallbackHandler;
            _unmanagedFnPtrForNativeCallback = Marshal.GetFunctionPointerForDelegate(nativeCallback);
        }

        public MsQuicTransportContext TransportContext { get; }
        public EndPoint EndPoint { get; set; }

        public IHostApplicationLifetime AppLifetime => TransportContext.AppLifetime;
        public IMsQuicTrace Log => TransportContext.Log;

        public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (await _acceptEnumerator.MoveNextAsync())
            {
                return _acceptEnumerator.Current;
            }

            return null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            StopAcceptingConnections();

            await UnbindAsync().ConfigureAwait(false);

            // TODO do something with _listener;

            //await StopThreadsAsync().ConfigureAwait(false);
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

        internal async Task BindAsync()
        {
            await StartAsync();

            _acceptEnumerator = AcceptConnectionsAsync();
        }

        private async IAsyncEnumerator<MsQuicConnection> AcceptConnectionsAsync()
        {
            while (true)
            {
                while (await _acceptConnectionQueue.Reader.WaitToReadAsync())
                {
                    while (_acceptConnectionQueue.Reader.TryRead(out var connection))
                    {
                        yield return connection;
                    }
                }

                yield return null;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _registration.RegistrationOpen(Encoding.ASCII.GetBytes(TransportContext.Options.RegistrationName));

            _secConfig = await _registration.CreateSecurityConfig(TransportContext.Options.Certificate);

            _session = _registration.SessionOpen(TransportContext.Options.Alpn);

            _nativeObjPtr = _session.ListenerOpen(NativeCallbackHandler);

            SetCallbackHandler(ListenerCallbackHandler);

            _session.SetIdleTimeout(TransportContext.Options.IdleTimeout);
            _session.SetPeerBiDirectionalStreamCount(TransportContext.Options.MaxBidirectionalStreamCount);
            _session.SetPeerUnidirectionalStreamCount(TransportContext.Options.MaxBidirectionalStreamCount);

            Start(EndPoint as IPEndPoint);
        }

        private uint ListenerCallbackHandler(
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

        internal delegate uint ListenerCallback(
            ref ListenerEvent evt);

        public void Start(IPEndPoint localIpEndpoint)
        {
            var address = MsQuicNativeMethods.Convert(localIpEndpoint);
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

            return quicListener.ExecuteCallback(ref connectionEventStruct);
        }

        private uint ExecuteCallback(
           ref ListenerEvent connectionEvent)
        {
            var status = MsQuicConstants.InternalError;
            try
            {
                status = _callback(ref connectionEvent);
            }
            catch (Exception)
            {
                // TODO log
            }
            return status;
        }

        internal void SetCallbackHandler(
            ListenerCallback callback)
        {
            _handle = GCHandle.Alloc(this);
            _callback = callback;
            _registration.SetCallbackHandlerDelegate(
                _nativeObjPtr,
                _unmanagedFnPtrForNativeCallback,
                GCHandle.ToIntPtr(_handle));
        }

        public void Stop()
        {
            _registration.ListenerStopDelegate(_nativeObjPtr);
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

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_nativeObjPtr != IntPtr.Zero)
            {
                _registration.ListenerCloseDelegate?.Invoke(_nativeObjPtr);
            }

            _nativeObjPtr = IntPtr.Zero;
            _registration = null;
            _disposed = true;
        }
    }
}
