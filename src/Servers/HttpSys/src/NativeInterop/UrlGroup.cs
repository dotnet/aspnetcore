// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed partial class UrlGroup : IDisposable
{
    private static readonly int BindingInfoSize =
        Marshal.SizeOf<HTTP_BINDING_INFO>();
    private static readonly int QosInfoSize =
        Marshal.SizeOf<HTTP_QOS_SETTING_INFO>();
    private static readonly int RequestPropertyInfoSize =
        Marshal.SizeOf<HTTP_BINDING_INFO>();

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

        _created = true;
        var statusCode = PInvoke.HttpCreateUrlGroup(_serverSession.Id.DangerousGetServerSessionId(), out var urlGroupId);

        if (statusCode != ErrorCodes.ERROR_SUCCESS)
        {
            throw new HttpSysException((int)statusCode);
        }

        Debug.Assert(urlGroupId != 0, "Invalid id returned by HttpCreateUrlGroup");
        Id = urlGroupId;
    }

    internal ulong Id { get; private set; }

    internal unsafe void SetMaxConnections(long maxConnections)
    {
        var connectionLimit = new HTTP_CONNECTION_LIMIT_INFO
        {
            Flags = HttpApi.HTTP_PROPERTY_FLAGS_PRESENT,
            MaxConnections = (uint)maxConnections
        };

        var qosSettings = new HTTP_QOS_SETTING_INFO
        {
            QosType = HTTP_QOS_SETTING_TYPE.HttpQosSettingTypeConnectionLimit,
            QosSetting = &connectionLimit
        };

        SetProperty(HTTP_SERVER_PROPERTY.HttpServerQosProperty, new IntPtr(&qosSettings), (uint)QosInfoSize);
    }

    internal unsafe void SetDelegationProperty(RequestQueue destination)
    {
        var propertyInfo = new HTTP_BINDING_INFO
        {
            Flags = HttpApi.HTTP_PROPERTY_FLAGS_PRESENT,
            RequestQueueHandle = (HANDLE)destination.Handle.DangerousGetHandle()
        };

        SetProperty(HTTP_SERVER_PROPERTY.HttpServerDelegationProperty, new IntPtr(&propertyInfo), (uint)RequestPropertyInfoSize);
    }

    internal unsafe void UnSetDelegationProperty(RequestQueue destination, bool throwOnError = true)
    {
        var propertyInfo = new HTTP_BINDING_INFO
        {
            RequestQueueHandle = (HANDLE)destination.Handle.DangerousGetHandle()
        };

        SetProperty(HTTP_SERVER_PROPERTY.HttpServerDelegationProperty, new IntPtr(&propertyInfo), (uint)RequestPropertyInfoSize, throwOnError);
    }

    internal unsafe void SetProperty(HTTP_SERVER_PROPERTY property, IntPtr info, uint infosize, bool throwOnError = true)
    {
        Debug.Assert(info != IntPtr.Zero, "SetUrlGroupProperty called with invalid pointer");
        CheckDisposed();

        var statusCode = PInvoke.HttpSetUrlGroupProperty(Id, property, info.ToPointer(), infosize);

        if (statusCode != ErrorCodes.ERROR_SUCCESS)
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

        var info = new HTTP_BINDING_INFO
        {
            Flags = HttpApi.HTTP_PROPERTY_FLAGS_PRESENT,
            RequestQueueHandle = (HANDLE)_requestQueue.Handle.DangerousGetHandle()
        };

        var infoptr = new IntPtr(&info);

        SetProperty(HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
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

        var info = new HTTP_BINDING_INFO();
        var infoptr = new IntPtr(&info);

        SetProperty(HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
            infoptr, (uint)BindingInfoSize, throwOnError: false);
    }

    internal void RegisterPrefix(string uriPrefix, int contextId)
    {
        Log.RegisteringPrefix(_logger, uriPrefix);
        CheckDisposed();
        var statusCode = PInvoke.HttpAddUrlToUrlGroup(Id, uriPrefix, (ulong)contextId);
        if (statusCode != ErrorCodes.ERROR_SUCCESS)
        {
            if (statusCode == ErrorCodes.ERROR_ALREADY_EXISTS)
            {
                // If we didn't create the queue and the uriPrefix already exists, confirm it exists for the
                // queue we attached to, if so we are all good, otherwise throw an already registered error.
                if (!_requestQueue.Created)
                {
                    unsafe
                    {
                        var findUrlStatusCode = PInvoke.HttpFindUrlGroupId(uriPrefix, _requestQueue.Handle, out var _);
                        if (findUrlStatusCode == ErrorCodes.ERROR_SUCCESS)
                        {
                            // Already registered for the desired queue, all good
                            return;
                        }
                    }
                }

                throw new HttpSysException((int)statusCode, Resources.FormatException_PrefixAlreadyRegistered(uriPrefix));
            }
            if (statusCode == ErrorCodes.ERROR_ACCESS_DENIED)
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

        PInvoke.HttpRemoveUrlFromUrlGroup(Id, uriPrefix, 0);
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

            var statusCode = PInvoke.HttpCloseUrlGroup(Id);

            if (statusCode != ErrorCodes.ERROR_SUCCESS)
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
