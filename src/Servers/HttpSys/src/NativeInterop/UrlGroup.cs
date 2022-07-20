// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed partial class UrlGroup : IDisposable
{
    private static readonly int BindingInfoSize =
        Marshal.SizeOf<HttpApiTypes.HTTP_BINDING_INFO>();
    private static readonly int QosInfoSize =
        Marshal.SizeOf<HttpApiTypes.HTTP_QOS_SETTING_INFO>();
    private static readonly int RequestPropertyInfoSize =
        Marshal.SizeOf<HttpApiTypes.HTTP_BINDING_INFO>();

    private readonly ILogger _logger;

    private readonly ServerSession? _serverSession;
    private readonly RequestQueue _requestQueue;
    private bool _disposed;
    private readonly bool _created;

    internal unsafe UrlGroup(ServerSession serverSession, RequestQueue requestQueue, ILogger logger)
    {
        _serverSession = serverSession;
        _requestQueue = requestQueue;
        _logger = logger;

        ulong urlGroupId = 0;
        _created = true;
        var statusCode = HttpApi.HttpCreateUrlGroup(
            _serverSession.Id.DangerousGetServerSessionId(), &urlGroupId, 0);

        if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
        {
            throw new HttpSysException((int)statusCode);
        }

        Debug.Assert(urlGroupId != 0, "Invalid id returned by HttpCreateUrlGroup");
        Id = urlGroupId;
    }

    internal ulong Id { get; private set; }

    internal unsafe void SetMaxConnections(long maxConnections)
    {
        var connectionLimit = new HttpApiTypes.HTTP_CONNECTION_LIMIT_INFO();
        connectionLimit.Flags = HttpApiTypes.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
        connectionLimit.MaxConnections = (uint)maxConnections;

        var qosSettings = new HttpApiTypes.HTTP_QOS_SETTING_INFO();
        qosSettings.QosType = HttpApiTypes.HTTP_QOS_SETTING_TYPE.HttpQosSettingTypeConnectionLimit;
        qosSettings.QosSetting = new IntPtr(&connectionLimit);

        SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerQosProperty, new IntPtr(&qosSettings), (uint)QosInfoSize);
    }

    internal unsafe void SetDelegationProperty(RequestQueue destination)
    {
        var propertyInfo = new HttpApiTypes.HTTP_BINDING_INFO();
        propertyInfo.Flags = HttpApiTypes.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
        propertyInfo.RequestQueueHandle = destination.Handle.DangerousGetHandle();

        SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerDelegationProperty, new IntPtr(&propertyInfo), (uint)RequestPropertyInfoSize);
    }

    internal unsafe void UnSetDelegationProperty(RequestQueue destination, bool throwOnError = true)
    {
        var propertyInfo = new HttpApiTypes.HTTP_BINDING_INFO();
        propertyInfo.Flags = HttpApiTypes.HTTP_FLAGS.NONE;
        propertyInfo.RequestQueueHandle = destination.Handle.DangerousGetHandle();

        SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerDelegationProperty, new IntPtr(&propertyInfo), (uint)RequestPropertyInfoSize, throwOnError);
    }

    internal void SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY property, IntPtr info, uint infosize, bool throwOnError = true)
    {
        Debug.Assert(info != IntPtr.Zero, "SetUrlGroupProperty called with invalid pointer");
        CheckDisposed();

        var statusCode = HttpApi.HttpSetUrlGroupProperty(Id, property, info, infosize);

        if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
        {
            var exception = new HttpSysException((int)statusCode);
            Log.SetUrlPropertyError(_logger, exception);
            if (throwOnError)
            {
                throw exception;
            }
        }
    }

    internal unsafe void AttachToQueue()
    {
        CheckDisposed();
        // Set the association between request queue and url group. After this, requests for registered urls will
        // get delivered to this request queue.

        var info = new HttpApiTypes.HTTP_BINDING_INFO();
        info.Flags = HttpApiTypes.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
        info.RequestQueueHandle = _requestQueue.Handle.DangerousGetHandle();

        var infoptr = new IntPtr(&info);

        SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
            infoptr, (uint)BindingInfoSize);
    }

    internal unsafe void DetachFromQueue()
    {
        CheckDisposed();
        // Break the association between request queue and url group. After this, requests for registered urls
        // will get 503s.
        // Note that this method may be called multiple times (Stop() and then Abort()). This
        // is fine since http.sys allows to set HttpServerBindingProperty multiple times for valid
        // Url groups.

        var info = new HttpApiTypes.HTTP_BINDING_INFO();
        info.Flags = HttpApiTypes.HTTP_FLAGS.NONE;
        info.RequestQueueHandle = IntPtr.Zero;

        var infoptr = new IntPtr(&info);

        SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
            infoptr, (uint)BindingInfoSize, throwOnError: false);
    }

    internal void RegisterPrefix(string uriPrefix, int contextId)
    {
        Log.RegisteringPrefix(_logger, uriPrefix);
        CheckDisposed();
        var statusCode = HttpApi.HttpAddUrlToUrlGroup(Id, uriPrefix, (ulong)contextId, 0);

        if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
        {
            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_ALREADY_EXISTS)
            {
                // If we didn't create the queue and the uriPrefix already exists, confirm it exists for the
                // queue we attached to, if so we are all good, otherwise throw an already registered error.
                if (!_requestQueue.Created)
                {
                    unsafe
                    {
                        ulong urlGroupId;
                        var findUrlStatusCode = HttpApi.HttpFindUrlGroupId(uriPrefix, _requestQueue.Handle, &urlGroupId);
                        if (findUrlStatusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
                        {
                            // Already registered for the desired queue, all good
                            return;
                        }
                    }
                }

                throw new HttpSysException((int)statusCode, Resources.FormatException_PrefixAlreadyRegistered(uriPrefix));
            }
            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_ACCESS_DENIED)
            {
                throw new HttpSysException((int)statusCode, Resources.FormatException_AccessDenied(uriPrefix, Environment.UserDomainName + @"\" + Environment.UserName));
            }
            throw new HttpSysException((int)statusCode);
        }
    }

    internal void UnregisterPrefix(string uriPrefix)
    {
        Log.UnregisteringPrefix(_logger, uriPrefix);
        CheckDisposed();

        HttpApi.HttpRemoveUrlFromUrlGroup(Id, uriPrefix, 0);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_created)
        {

            Debug.Assert(Id != 0, "HttpCloseUrlGroup called with invalid url group id");

            uint statusCode = HttpApi.HttpCloseUrlGroup(Id);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                Log.CloseUrlGroupError(_logger, statusCode);
            }

        }

        Id = 0;
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
