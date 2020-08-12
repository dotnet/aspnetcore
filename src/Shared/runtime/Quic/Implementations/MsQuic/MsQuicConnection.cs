// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.IO;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.ExceptionServices;
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
        private readonly MsQuicSession? _session;

        // Pointer to the underlying connection
        // TODO replace all IntPtr with SafeHandles
        private IntPtr _ptr;

        // Handle to this object for native callbacks.
        private GCHandle _handle;

        // Delegate that wraps the static function that will be called when receiving an event.
        internal static readonly ConnectionCallbackDelegate s_connectionDelegate = new ConnectionCallbackDelegate(NativeCallbackHandler);

        // Endpoint to either connect to or the endpoint already accepted.
        private IPEndPoint? _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;

        private SslApplicationProtocol _negotiatedAlpnProtocol;

        // TODO: only allocate these when there is an outstanding connect/shutdown.
        private readonly TaskCompletionSource<uint> _connectTcs = new TaskCompletionSource<uint>();
        private readonly TaskCompletionSource<uint> _shutdownTcs = new TaskCompletionSource<uint>();

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
            _localEndPoint = localEndPoint;
            _remoteEndPoint = remoteEndPoint;
            _ptr = nativeObjPtr;
            _connected = true;

            SetCallbackHandler();
            SetIdleTimeout(TimeSpan.FromSeconds(120));
        }

        // constructor for outbound connections
        public MsQuicConnection(QuicClientConnectionOptions options)
        {
            // TODO need to figure out if/how we want to expose sessions
            // Creating a session per connection isn't ideal.
            _session = new MsQuicSession();
            _ptr = _session.ConnectionOpen(options);
            _remoteEndPoint = options.RemoteEndPoint!;

            SetCallbackHandler();
            SetIdleTimeout(options.IdleTimeout);
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
            _securityConfig = await MsQuicApi.Api.CreateSecurityConfig(cert, certFilePath, privateKeyFilePath).ConfigureAwait(false);
            // TODO this isn't being set correctly
            MsQuicParameterHelpers.SetSecurityConfig(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.SEC_CONFIG, _securityConfig!.NativeObjPtr);
        }

        internal override IPEndPoint RemoteEndPoint => new IPEndPoint(_remoteEndPoint.Address, _remoteEndPoint.Port);

        internal override SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedAlpnProtocol;

        internal override bool Connected => _connected;

        internal uint HandleEvent(ref ConnectionEvent connectionEvent)
        {
            try
            {
                switch (connectionEvent.Type)
                {
                    case QUIC_CONNECTION_EVENT.CONNECTED:
                        return HandleEventConnected(ref connectionEvent);
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_TRANSPORT:
                        return HandleEventShutdownInitiatedByTransport(ref connectionEvent);
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_INITIATED_BY_PEER:
                        return HandleEventShutdownInitiatedByPeer(ref connectionEvent);
                    case QUIC_CONNECTION_EVENT.SHUTDOWN_COMPLETE:
                        return HandleEventShutdownComplete(ref connectionEvent);
                    case QUIC_CONNECTION_EVENT.PEER_STREAM_STARTED:
                        return HandleEventNewStream(ref connectionEvent);
                    case QUIC_CONNECTION_EVENT.STREAMS_AVAILABLE:
                        return HandleEventStreamsAvailable(ref connectionEvent);
                    default:
                        return MsQuicStatusCodes.Success;
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

        private uint HandleEventConnected(ref ConnectionEvent connectionEvent)
        {
            if (!_connected)
            {
                // _connected will already be true for connections accepted from a listener.

                SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, _ptr, (uint)QUIC_PARAM_LEVEL.CONNECTION, (uint)QUIC_PARAM_CONN.LOCAL_ADDRESS);
                _localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref inetAddress);

                SetNegotiatedAlpn(connectionEvent.Data.Connected.NegotiatedAlpn, connectionEvent.Data.Connected.NegotiatedAlpnLength);

                _connected = true;
                _connectTcs.SetResult(MsQuicStatusCodes.Success);
            }

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownInitiatedByTransport(ref ConnectionEvent connectionEvent)
        {
            if (!_connected)
            {
                _connectTcs.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException("Connection has been shutdown.")));
            }

            _acceptQueue.Writer.Complete();

            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownInitiatedByPeer(ref ConnectionEvent connectionEvent)
        {
            _abortErrorCode = connectionEvent.Data.ShutdownInitiatedByPeer.ErrorCode;
            _acceptQueue.Writer.Complete();
            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventShutdownComplete(ref ConnectionEvent connectionEvent)
        {
            _shutdownTcs.SetResult(MsQuicStatusCodes.Success);
            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventNewStream(ref ConnectionEvent connectionEvent)
        {
            MsQuicStream msQuicStream = new MsQuicStream(this, connectionEvent.StreamFlags, connectionEvent.Data.StreamStarted.Stream, inbound: true);
            _acceptQueue.Writer.TryWrite(msQuicStream);
            return MsQuicStatusCodes.Success;
        }

        private uint HandleEventStreamsAvailable(ref ConnectionEvent connectionEvent)
        {
            return MsQuicStatusCodes.Success;
        }

        internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default)
        {
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

            return new ValueTask(_connectTcs.Task);
        }

        private MsQuicStream StreamOpen(
            QUIC_STREAM_OPEN_FLAG flags)
        {
            IntPtr streamPtr = IntPtr.Zero;
            QuicExceptionHelpers.ThrowIfFailed(
                MsQuicApi.Api.StreamOpenDelegate(
                _ptr,
                (uint)flags,
                MsQuicStream.s_streamDelegate,
                IntPtr.Zero,
                out streamPtr),
                "Failed to open stream to peer.");

            return new MsQuicStream(this, flags, streamPtr, inbound: false);
        }

        private void SetCallbackHandler()
        {
            Debug.Assert(!_handle.IsAllocated);
            _handle = GCHandle.Alloc(this);

            MsQuicApi.Api.SetCallbackHandlerDelegate(
                _ptr,
                s_connectionDelegate,
                GCHandle.ToIntPtr(_handle));
        }

        private ValueTask ShutdownAsync(
            QUIC_CONNECTION_SHUTDOWN_FLAG Flags,
            long ErrorCode)
        {
            uint status = MsQuicApi.Api.ConnectionShutdownDelegate(
                _ptr,
                (uint)Flags,
                ErrorCode);
            QuicExceptionHelpers.ThrowIfFailed(status, "Failed to shutdown connection.");

            return new ValueTask(_shutdownTcs.Task);
        }

        internal void SetNegotiatedAlpn(IntPtr alpn, int alpnLength)
        {
            if (alpn != IntPtr.Zero && alpnLength != 0)
            {
                var buffer = new byte[alpnLength];
                Marshal.Copy(alpn, buffer, 0, alpnLength);
                _negotiatedAlpnProtocol = new SslApplicationProtocol(buffer);
            }
        }

        private static uint NativeCallbackHandler(
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
