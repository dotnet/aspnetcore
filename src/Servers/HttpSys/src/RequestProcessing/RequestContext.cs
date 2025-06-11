// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;
using static Microsoft.AspNetCore.HttpSys.Internal.HttpApiTypes;
using static Microsoft.AspNetCore.HttpSys.Internal.UnsafeNclNativeMethods;
using static Microsoft.AspNetCore.Server.HttpSys.HttpSysOptions;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal partial class RequestContext : NativeRequestContext, IThreadPoolWorkItem
{
    private static readonly Action<object?> AbortDelegate = Abort;
    private CancellationTokenSource? _requestAbortSource;
    private CancellationToken? _disconnectToken;
    private bool _disposed;
    private bool _initialized;

    public RequestContext(HttpSysListener server, uint? bufferSize, ulong requestId)
        : base(server.MemoryPool, bufferSize, requestId, server.Options.UseLatin1RequestHeaders)
    {
        Server = server;
        AllowSynchronousIO = server.Options.AllowSynchronousIO;
    }

    internal HttpSysListener Server { get; }

    internal ILogger Logger => Server.Logger;

    public Request Request { get; private set; } = default!;

    public Response Response { get; private set; } = default!;

    public WindowsPrincipal User => Request.User;

    public CancellationToken DisconnectToken
    {
        get
        {
            // Create a new token per request, but link it to a single connection token.
            // We need to be able to dispose of the registrations each request to prevent leaks.
            if (!_disconnectToken.HasValue)
            {
                if (_disposed || Response.BodyIsFinished)
                {
                    // We cannot register for disconnect notifications after the response has finished sending.
                    _disconnectToken = CancellationToken.None;
                }
                else
                {
                    var connectionDisconnectToken = Server.DisconnectListener.GetTokenForConnection(Request.UConnectionId);

                    if (connectionDisconnectToken.CanBeCanceled)
                    {
                        _requestAbortSource = CancellationTokenSource.CreateLinkedTokenSource(connectionDisconnectToken);
                        _disconnectToken = _requestAbortSource.Token;
                    }
                    else
                    {
                        _disconnectToken = CancellationToken.None;
                    }
                }
            }
            return _disconnectToken.Value;
        }
    }

    public unsafe Guid TraceIdentifier
    {
        get
        {
            // This is the base GUID used by HTTP.SYS for generating the activity ID.
            // HTTP.SYS overwrites the first 8 bytes of the base GUID with RequestId to generate ETW activity ID.
            var guid = new Guid(0xffcb4c93, 0xa57f, 0x453c, 0xb6, 0x3f, 0x84, 0x71, 0xc, 0x79, 0x67, 0xbb);
            *((ulong*)&guid) = Request.RequestId;
            return guid;
        }
    }

    public bool IsUpgradableRequest => Request.IsUpgradable;

    internal bool AllowSynchronousIO { get; set; }

    public Task<Stream> UpgradeAsync()
    {
        if (!IsUpgradableRequest)
        {
            if (Request.ProtocolVersion != System.Net.HttpVersion.Version11)
            {
                throw new InvalidOperationException("Upgrade requires HTTP/1.1.");
            }
            throw new InvalidOperationException("This request cannot be upgraded because it has a body.");
        }
        if (Response.HasStarted)
        {
            throw new InvalidOperationException("This request cannot be upgraded, the response has already started.");
        }

        // Set the status code and reason phrase
        Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
        Response.ReasonPhrase = HttpReasonPhrase.Get(StatusCodes.Status101SwitchingProtocols);

        Response.SendOpaqueUpgrade(); // TODO: Async
        Request.SwitchToOpaqueMode();
        Response.SwitchToOpaqueMode();
        var opaqueStream = new OpaqueStream(Request.Body, Response.Body);
        return Task.FromResult<Stream>(opaqueStream);
    }

    // TODO: Public when needed
    internal bool TryGetChannelBinding(ref ChannelBinding? value)
    {
        if (!Request.IsHttps)
        {
            Log.ChannelBindingNeedsHttps(Logger);
            return false;
        }

        value = ClientCertLoader.GetChannelBindingFromTls(Server.RequestQueue, Request.UConnectionId, Logger);

        Debug.Assert(value != null, "GetChannelBindingFromTls returned null even though OS supposedly supports Extended Protection");
        Log.ChannelBindingRetrieved(Logger);
        return value != null;
    }

    /// <summary>
    /// Flushes and completes the response.
    /// </summary>
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_initialized)
        {
            // TODO: Verbose log
            try
            {
                _requestAbortSource?.Dispose();
                Response.Dispose();
            }
            catch
            {
                Abort();
            }
            finally
            {
                Request.Dispose();
            }
        }

        base.Dispose();
    }

    /// <summary>
    /// Forcibly terminate and dispose the request, closing the connection if necessary.
    /// </summary>
    public void Abort()
    {
        // May be called from Dispose() code path, don't check _disposed.
        // TODO: Verbose log
        _disposed = true;
        if (_requestAbortSource != null)
        {
            try
            {
                _requestAbortSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Log.AbortError(Logger, ex);
            }
            _requestAbortSource.Dispose();
        }
        else
        {
            _disconnectToken = new CancellationToken(canceled: true);
        }
        ForceCancelRequest();
        // Request and/or Response can be null (even though the property doesn't say it can)
        // if the constructor throws (can happen for invalid path format)
        Request?.Dispose();
        // Only Abort, Response.Dispose() tries a graceful flush
        Response?.Abort();
    }

    private static void Abort(object? state)
    {
        var context = (RequestContext)state!;
        context.Abort();
    }

    internal CancellationTokenRegistration RegisterForCancellation(CancellationToken cancellationToken)
    {
        return cancellationToken.Register(AbortDelegate, this);
    }

    // The request is being aborted, but large writes may be in progress. Cancel them.
    internal void ForceCancelRequest()
    {
        try
        {
            // Shouldn't be able to get here when this is null, but just in case we'll noop
            if (_requestId is null)
            {
                return;
            }

            var statusCode = HttpApi.HttpCancelHttpRequest(Server.RequestQueue.Handle,
                _requestId.Value, default);

            // Either the connection has already dropped, or the last write is in progress.
            // The requestId becomes invalid as soon as the last Content-Length write starts.
            // The only way to cancel now is with CancelIoEx.
            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_CONNECTION_INVALID)
            {
                // Can be null if processing the request threw and the response object was never created.
                Response?.CancelLastWrite();
            }
        }
        catch (ObjectDisposedException)
        {
            // RequestQueueHandle may have been closed
        }
    }

    /// <summary>
    /// Attempts to get the client hello message bytes from the http.sys.
    /// If successful writes the bytes into <paramref name="destination"/>, and shows how many bytes were written in <paramref name="bytesReturned"/>.
    /// If not successful because <paramref name="destination"/> is not large enough, returns false and shows a size of <paramref name="destination"/> required in <paramref name="bytesReturned"/>.
    /// If not successful for other reason - throws exception with message/errorCode.
    /// </summary>
    internal unsafe bool TryGetTlsClientHelloMessageBytes(
        Span<byte> destination,
        out int bytesReturned)
    {
        bytesReturned = default;
        if (!HttpApi.SupportsClientHello)
        {
            // not supported, so we just return and don't invoke the callback
            throw new InvalidOperationException("Windows HTTP Server API does not support HTTP_FEATURE_ID.HttpFeatureCacheTlsClientHello or HttpQueryRequestProperty. See HTTP_FEATURE_ID for details.");
        }

        uint statusCode;
        var requestId = PinsReleased ? Request.RequestId : RequestId;

        uint bytesReturnedValue = 0;
        uint* bytesReturnedPointer = &bytesReturnedValue;

        fixed (byte* pBuffer = destination)
        {
            statusCode = HttpApi.HttpGetRequestProperty(
                requestQueueHandle: Server.RequestQueue.Handle,
                requestId,
                propertyId: (HTTP_REQUEST_PROPERTY)11 /* HTTP_REQUEST_PROPERTY.HttpRequestPropertyTlsClientHello  */,
                qualifier: null,
                qualifierSize: 0,
                output: pBuffer,
                outputSize: (uint)destination.Length,
                bytesReturned: bytesReturnedPointer,
                overlapped: IntPtr.Zero);

            bytesReturned = checked((int)bytesReturnedValue);

            if (statusCode is ErrorCodes.ERROR_SUCCESS)
            {
                return true;
            }

            // if buffer supplied is too small, `bytesReturned` has proper size
            if (statusCode is ErrorCodes.ERROR_MORE_DATA or ErrorCodes.ERROR_INSUFFICIENT_BUFFER)
            {
                return false;
            }
        }

        Log.TlsClientHelloRetrieveError(Logger, requestId, statusCode);
        throw new HttpSysException((int)statusCode);
    }

    /// <summary>
    /// Attempts to get the client hello message bytes from HTTP.sys and calls the user provided callback.
    /// If not successful, will return false.
    /// </summary>
    internal unsafe bool GetAndInvokeTlsClientHelloMessageBytesCallback(IFeatureCollection features, TlsClientHelloCallback tlsClientHelloBytesCallback)
    {
        if (!HttpApi.SupportsClientHello)
        {
            // not supported, so we just return and don't invoke the callback
            return false;
        }

        uint bytesReturnedValue = 0;
        uint* bytesReturned = &bytesReturnedValue;
        uint statusCode;

        var requestId = PinsReleased ? Request.RequestId : RequestId;

        // we will try with some "random" buffer size
        var buffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            fixed (byte* pBuffer = buffer)
            {
                statusCode = HttpApi.HttpGetRequestProperty(
                    requestQueueHandle: Server.RequestQueue.Handle,
                    requestId,
                    propertyId: (HTTP_REQUEST_PROPERTY)11 /* HTTP_REQUEST_PROPERTY.HttpRequestPropertyTlsClientHello  */,
                    qualifier: null,
                    qualifierSize: 0,
                    output: pBuffer,
                    outputSize: (uint)buffer.Length,
                    bytesReturned: bytesReturned,
                    overlapped: IntPtr.Zero);

                if (statusCode is ErrorCodes.ERROR_SUCCESS)
                {
                    tlsClientHelloBytesCallback(features, buffer.AsSpan(0, (int)bytesReturnedValue));
                    return true;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        // if buffer supplied is too small, `bytesReturned` will have proper size
        // so retry should succeed with the properly allocated buffer
        if (statusCode is ErrorCodes.ERROR_MORE_DATA or ErrorCodes.ERROR_INSUFFICIENT_BUFFER)
        {
            try
            {
                var correctSize = (int)bytesReturnedValue;
                buffer = ArrayPool<byte>.Shared.Rent(correctSize);

                fixed (byte* pBuffer = buffer)
                {
                    statusCode = HttpApi.HttpGetRequestProperty(
                        requestQueueHandle: Server.RequestQueue.Handle,
                        requestId,
                        propertyId: (HTTP_REQUEST_PROPERTY)11 /* HTTP_REQUEST_PROPERTY.HttpRequestPropertyTlsClientHello  */,
                        qualifier: null,
                        qualifierSize: 0,
                        output: pBuffer,
                        outputSize: (uint)buffer.Length,
                        bytesReturned: bytesReturned,
                        overlapped: IntPtr.Zero);

                    if (statusCode is ErrorCodes.ERROR_SUCCESS)
                    {
                        tlsClientHelloBytesCallback(features, buffer.AsSpan(0, correctSize));
                        return true;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }

        Log.TlsClientHelloRetrieveError(Logger, requestId, statusCode);
        return false;
    }

    internal unsafe HTTP_REQUEST_PROPERTY_SNI GetClientSni()
    {
        if (!HttpApi.HttpGetRequestPropertySupported)
        {
            return default;
        }

        var buffer = new byte[HttpApiTypes.SniPropertySizeInBytes];
        fixed (byte* pBuffer = buffer)
        {
            var statusCode = HttpApi.HttpGetRequestProperty(
                Server.RequestQueue.Handle,
                RequestId,
                HTTP_REQUEST_PROPERTY.HttpRequestPropertySni,
                qualifier: null,
                qualifierSize: 0,
                pBuffer,
                (uint)buffer.Length,
                bytesReturned: null,
                IntPtr.Zero);

            if (statusCode == ErrorCodes.ERROR_SUCCESS)
            {
                return Marshal.PtrToStructure<HTTP_REQUEST_PROPERTY_SNI>((IntPtr)pBuffer);
            }
        }

        return default;
    }

    // You must still call ForceCancelRequest after this.
    internal unsafe void SetResetCode(int errorCode)
    {
        if (!HttpApi.SupportsReset)
        {
            return;
        }

        try
        {
            var streamError = new HttpApiTypes.HTTP_REQUEST_PROPERTY_STREAM_ERROR() { ErrorCode = (uint)errorCode };
            var statusCode = HttpApi.HttpSetRequestProperty(Server.RequestQueue.Handle, Request.RequestId, HttpApiTypes.HTTP_REQUEST_PROPERTY.HttpRequestPropertyStreamError, (void*)&streamError,
                (uint)sizeof(HttpApiTypes.HTTP_REQUEST_PROPERTY_STREAM_ERROR), IntPtr.Zero);
        }
        catch (ObjectDisposedException)
        {
            // RequestQueueHandle may have been closed
        }
    }

    public virtual Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }

    public void Execute()
    {
        _ = ExecuteAsync();
    }

    protected void SetFatalResponse(int status)
    {
        Response.StatusCode = status;
        Response.ContentLength = 0;
    }

    internal unsafe void Delegate(DelegationRule destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (Request.HasRequestBodyStarted)
        {
            throw new InvalidOperationException("This request cannot be delegated, the request body has already started.");
        }
        if (Response.HasStarted)
        {
            throw new InvalidOperationException("This request cannot be delegated, the response has already started.");
        }

        var source = Server.RequestQueue;

        uint statusCode;

        fixed (char* uriPointer = destination.UrlPrefix)
        {
            var property = new HttpApiTypes.HTTP_DELEGATE_REQUEST_PROPERTY_INFO()
            {
                PropertyId = HttpApiTypes.HTTP_DELEGATE_REQUEST_PROPERTY_ID.DelegateRequestDelegateUrlProperty,
                PropertyInfo = (IntPtr)uriPointer,
                PropertyInfoLength = (uint)System.Text.Encoding.Unicode.GetByteCount(destination.UrlPrefix)
            };

            // Passing 0 for delegateUrlGroupId allows http.sys to find the right group for the
            // URL passed in via the property above. If we passed in the receiver's URL group id
            // instead of 0, then delegation would fail if the receiver restarted.
            statusCode = HttpApi.HttpDelegateRequestEx(source.Handle,
                                                           destination.Queue.Handle,
                                                           Request.RequestId,
                                                           delegateUrlGroupId: 0,
                                                           propertyInfoSetSize: 1,
                                                           &property);
        }

        if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
        {
            throw new HttpSysException((int)statusCode);
        }

        Response.MarkDelegated();
    }
}
