// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.IO;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Quic.Implementations.MsQuic.Internal.MsQuicNativeMethods;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicConnection : QuicConnectionProvider
    {
        private MsQuicSession? _session;

        // Pointer to the underlying connection
        // TODO replace all IntPtr with SafeHandles
        private IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        // TODO investigate if the delegate can be static instead.
        private ConnectionCallbackDelegate? _connectionDelegate;

        // Endpoint to either connect to or the endpoint already accepted.
        private IPEndPoint? _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;

        private readonly ResettableCompletionSource<uint> _connectTcs = new ResettableCompletionSource<uint>();
        private readonly ResettableCompletionSource<uint> _shutdownTcs = new ResettableCompletionSource<uint>();

        private bool _disposed;
        private bool _connected;
        private MsQuicSecurityConfig? _securityConfig;
        private long _abortErrorCode = -1;

        // Queue for accepted streams
        private readonly Channel<MsQuicStream> _acceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions()
        {
            SingleReader = true,
            SingleWriter = true,
        });

        // constructor for inbound connections
        public MsQuicConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, IntPtr nativeObjPtr)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;
            _ptr = nativeObjPtr;

            SetCallbackHandler();
            SetIdleTimeout(TimeSpan.FromSeconds(120));
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        // constructor for outbound connections
        public MsQuicConnection(QuicClientConnectionOptions options)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            // TODO need to figure out if/how we want to expose sessions
            // Creating a session per connection isn't ideal.
            _session = new MsQuicSession();
            _ptr = _session.ConnectionOpen(options);
            _remoteEndPoint = options.RemoteEndPoint!;

            SetCallbackHandler();
            SetIdleTimeout(options.IdleTimeout);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override IPEndPoint LocalEndPoint
        {
            get
            {
                return new IPEndPoint(_localEndPoint!.Address, _localEndPoint.Port);
            }
        }

        internal async ValueTask SetSecurityConfigForConnection(X509Certificate cert, string? certFilePath, string? privateKeyFilePath)
        {
            _securityConfig = await MsQuicApi.Api.CreateSecurityConfig(cert, certFilePath, privateKeyFilePath);
            // TODO this isn't being set correctly
            MsQuicParameterHelpers.SetSecurityConfig(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.SEC_CONFIG, _securityConfig!.NativeObjPtr);
        }

        internal override IPEndPoint RemoteEndPoint => new IPEndPoint(_remoteEndPoint.Address, _remoteEndPoint.Port);

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => throw new NotImplementedException();

        internal override bool Connected => _connected;

        internal uint HandleEvent(ref ConnectionEvent connectionEvent)
        {
            uint status = MsQuicStatusCodes.Success;
            try
            {
                switch (connectionEvent.Type)
                {
                    // Connection is connected, can start to create streams.
                    case QUIC_CONNECTION_EVENT.CONNECTED:
                        {
                            status = HandleEventConnected(
                                connectionEvent);
                        }
                        break;

                    // Connection is being closed by the transport
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_TRANSPORT:
                        {
                            status = HandleEventShutdownInitiatedByTransport(
                                connectionEvent);
                        }
                        break;

                    // Connection is being closed by the peer
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_PEER:
                        {
                            status = HandleEventShutdownInitiatedByPeer(
                                connectionEvent);
                        }
                        break;

                    // Connection has been shutdown
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_COMPLETE:
                        {
                            status = HandleEventShutdownComplete(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.PEER_STREAM_STARTED:
                        {
                            status = HandleEventNewStream(
                                connectionEvent);
                        }
                        break;

                    case QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE:
                        {
                            status = HandleEventStreamsAvailable(
                                connectionEvent);
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception)
            {
                // TODO we may want to either add a debug assert here or return specific error codes
                // based on the exception caught.
                return MsQuicStatusCodes.InternalError;
            }

            return status;
        }

        private uint HandleEventConnected(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.LOCAL_ADDRESS);
            _localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(inetAddress);

            _connected = true;
            // I don't believe we need to lock here because
            // handle event connected will not be called at the same time as
            // handle event shutdown initiated by transport
            _connectTcs.Complete(MsQuicStatusCodes.Success);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownInitiatedByTransport(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (!_connected)
            {
                _connectTcs.CompleteException(new IOException("Connection has been shutdown."));
            }

            _acceptQueue.Writer.Complete();

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownInitiatedByPeer(ConnectionEvent connectionEvent)
        {
            _abortErrorCode = connectionEvent.Data.ShutdownBeginPeer.ErrorCode;
            _acceptQueue.Writer.Complete();
            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownComplete(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            _shutdownTcs.Complete(MsQuicStatusCodes.Success);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventNewStream(ConnectionEvent connectionEvent)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            MsQuicStream msQuicStream = new MsQuicStream(this, connectionEvent.StreamFlags, connectionEvent.Data.NewStream.Stream, inbound: true);

            _acceptQueue.Writer.TryWrite(msQuicStream);
            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventStreamsAvailable(ConnectionEvent connectionEvent)
        {
            return MsQuicStatusCodes.Success;
        }

        internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            ThrowIfDisposed();

            MsQuicStream stream;

            try
            {
                stream = await _acceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                throw _abortErrorCode switch
                {
                    -1 => new QuicOperationAbortedException(), // Shutdown initiated by us.
                    long err => new QuicConnectionAbortedException(err) // Shutdown initiated by peer.
                };
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return stream;
        }

        internal override QuicStreamProvider OpenUnidirectionalStream()
        {
            ThrowIfDisposed();

            return StreamOpen(QUIC_STREAM_OPEN_FLAG.UNIDIRECTIONAL);
        }

        internal override QuicStreamProvider OpenBidirectionalStream()
        {
            ThrowIfDisposed();

            return StreamOpen(QUIC_STREAM_OPEN_FLAG.NONE);
        }

        internal override long GetRemoteAvailableUnidirectionalStreamCount()
        {
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.PEER_UNIDI_STREAM_COUNT);
        }

        internal override long GetRemoteAvailableBidirectionalStreamCount()
        {
            return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.PEER_BIDI_STREAM_COUNT);
        }

        private unsafe void SetIdleTimeout(TimeSpan timeout)
        {
            MsQuicParameterHelpers.SetULongParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.IDLE_TIMEOUT, (ulong)timeout.TotalMilliseconds);
        }

        internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            QuicExceptionHelpers.ThrowIfFailed(
                MsQuicApi.Api.ConnectionStartDelegate(
                _ptr,
                (ushort)_remoteEndPoint.AddressFamily,
                _remoteEndPoint.Address.ToString(),
                (ushort)_remoteEndPoint.Port),
                "Failed to connect to peer.");

            return _connectTcs.GetTypelessValueTask();
        }

        private MsQuicStream StreamOpen(
            QUIC_STREAM_OPEN_FLAG flags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            IntPtr streamPtr = IntPtr.Zero;
            QuicExceptionHelpers.ThrowIfFailed(
                MsQuicApi.Api.StreamOpenDelegate(
                _ptr,
                (uint)flags,
                MsQuicStream.NativeCallbackHandler,
                IntPtr.Zero,
                out streamPtr),
                "Failed to open stream to peer.");

            MsQuicStream stream = new MsQuicStream(this, flags, streamPtr, inbound: false);

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return stream;
        }

        private void SetCallbackHandler()
        {
            _handle = GCHandle.Alloc(this);
            _connectionDelegate = new ConnectionCallbackDelegate(NativeCallbackHandler);
            MsQuicApi.Api.SetCallbackHandlerDelegate(
                _ptr,
                _connectionDelegate,
                GCHandle.ToIntPtr(_handle));
        }

        private ValueTask ShutdownAsync(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            long ErrorCode)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            uint status = MsQuicApi.Api.ConnectionShutdownDelegate(
                _ptr,
                (uint)Flags,
                ErrorCode);
            QuicExceptionHelpers.ThrowIfFailed(status, "Failed to shutdown connection.");

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            return _shutdownTcs.GetTypelessValueTask();
        }

        internal static uint NativeCallbackHandler(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEventStruct)
        {
            GCHandle handle = GCHandle.FromIntPtr(context);
            MsQuicConnection quicConnection = (MsQuicConnection)handle.Target!;
            return quicConnection.HandleEvent(ref connectionEventStruct);
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicConnection()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Enter(this);

            if (_ptr != IntPtr.Zero)
            {
                MsQuicApi.Api.ConnectionCloseDelegate?.Invoke(_ptr);
            }

            _ptr = IntPtr.Zero;

            if (disposing)
            {
                _handle.Free();
                _session?.Dispose();
                _securityConfig?.Dispose();
            }

            _disposed = true;

            if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
        }

        internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAG.NONE, errorCode);
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
