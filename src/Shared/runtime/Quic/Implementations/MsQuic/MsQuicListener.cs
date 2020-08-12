// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicListener : QuicListenerProvider, IDisposable
    {
        // Security configuration for MsQuic
        private readonly MsQuicSession _session;

        // Pointer to the underlying listener
        // TODO replace all IntPtr with SafeHandles
        private IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        private static readonly ListenerCallbackDelegate s_listenerDelegate = new ListenerCallbackDelegate(NativeCallbackHandler);

        // Ssl listening options (ALPN, cert, etc)
        private readonly SslServerAuthenticationOptions _sslOptions;

        private QuicListenerOptions _options;
        private volatile bool _disposed;
        private IPEndPoint _listenEndPoint;

        private readonly Channel<MsQuicConnection> _acceptConnectionQueue;

        internal MsQuicListener(QuicListenerOptions options)
        {
            _session = new MsQuicSession();
            _acceptConnectionQueue = Channel.CreateBounded<MsQuicConnection>(new BoundedChannelOptions(options.ListenBacklog)
            {
                SingleReader = true,
                SingleWriter = true
            });

            _options = options;
            _sslOptions = options.ServerAuthenticationOptions!;
            _listenEndPoint = options.ListenEndPoint!;

            _ptr = _session.ListenerOpen(options);
        }

        internal override IPEndPoint ListenEndPoint
        {
            get
            {
                return new IPEndPoint(_listenEndPoint.Address, _listenEndPoint.Port);
            }
        }

        internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            MsQuicConnection connection;

            try
            {
                connection = await _acceptConnectionQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                throw new QuicOperationAbortedException();
            }

            await connection.SetSecurityConfigForConnection(_sslOptions.ServerCertificate!,
                _options.CertificateFilePath,
                _options.PrivateKeyFilePath).ConfigureAwait(false);

            return connection;
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicListener()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            StopAcceptingConnections();

            if (_ptr != IntPtr.Zero)
            {
                MsQuicApi.Api.ListenerStopDelegate(_ptr);
                MsQuicApi.Api.ListenerCloseDelegate(_ptr);
            }

            _ptr = IntPtr.Zero;

            // TODO this call to session dispose hangs.
            //_session.Dispose();
            _disposed = true;
        }

        internal override void Start()
        {
            ThrowIfDisposed();

            SetCallbackHandler();

            SOCKADDR_INET address = MsQuicAddressHelpers.IPEndPointToINet(_listenEndPoint);

            QuicExceptionHelpers.ThrowIfFailed(MsQuicApi.Api.ListenerStartDelegate(
                _ptr,
                ref address),
                "Failed to start listener.");

            SetListenPort();
        }

        internal override void Close()
        {
            ThrowIfDisposed();

            MsQuicApi.Api.ListenerStopDelegate(_ptr);
        }

        private unsafe void SetListenPort()
        {
            SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.LISTENER, (uint)QUIC_PARAM_LISTENER.LOCAL_ADDRESS);

            _listenEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref inetAddress);
        }

        internal unsafe uint ListenerCallbackHandler(ref ListenerEvent evt)
        {
            try
            {
                switch (evt.Type)
                {
                    case QUIC_LISTENER_EVENT.NEW_CONNECTION:
                        {
                            ref NewConnectionInfo connectionInfo = ref *(NewConnectionInfo*)evt.Data.NewConnection.Info;

                            IPEndPoint localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(SOCKADDR_INET*)connectionInfo.LocalAddress);
                            IPEndPoint remoteEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(SOCKADDR_INET*)connectionInfo.RemoteAddress);

                            MsQuicConnection msQuicConnection = new MsQuicConnection(localEndPoint, remoteEndPoint, evt.Data.NewConnection.Connection);
                            msQuicConnection.SetNegotiatedAlpn(connectionInfo.NegotiatedAlpn, connectionInfo.NegotiatedAlpnLength);

                            _acceptConnectionQueue.Writer.TryWrite(msQuicConnection);
                        }
                        // Always pend the new connection to wait for the security config to be resolved
                        // TODO this doesn't need to be async always
                        return MsQuicStatusCodes.Pending;
                    default:
                        return MsQuicStatusCodes.InternalError;
                }
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Error(this, $"Exception occurred during connection callback: {ex.Message}");
                }

                // TODO: trigger an exception on any outstanding async calls.

                return MsQuicStatusCodes.InternalError;
            }
        }

        private void StopAcceptingConnections()
        {
            _acceptConnectionQueue.Writer.TryComplete();
        }

        internal static uint NativeCallbackHandler(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent connectionEventStruct)
        {
            GCHandle handle = GCHandle.FromIntPtr(context);
            MsQuicListener quicListener = (MsQuicListener)handle.Target!;

            return quicListener.ListenerCallbackHandler(ref connectionEventStruct);
        }

        internal void SetCallbackHandler()
        {
            Debug.Assert(!_handle.IsAllocated);
            _handle = GCHandle.Alloc(this);

            MsQuicApi.Api.SetCallbackHandlerDelegate(
                _ptr,
                s_listenerDelegate,
                GCHandle.ToIntPtr(_handle));
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MsQuicStream));
            }
        }
    }
}
