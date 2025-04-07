// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.HttpSys.RequestProcessing;

internal sealed class TlsListener
{
    private readonly HttpSysOptions _httpSysOptions;
    private readonly ConcurrentDictionary<ulong, bool> _connectionIdsTlsClientHelloCallbackInvokedMap = new();

    internal TlsListener(HttpSysOptions httpSysOptions)
    {
        _httpSysOptions = httpSysOptions;
    }

    internal void InvokeTlsClientHelloCallback(IFeatureCollection features, Request request)
    {
        if (_connectionIdsTlsClientHelloCallbackInvokedMap.TryGetValue(request.UConnectionId, out _))
        {
            // invoking TLS client hello callback per request on same connection is what we are trying to avoid
            return;
        }

        var success = request.GetAndInvokeTlsClientHelloCallback(features, _httpSysOptions.TlsClientHelloBytesCallback);
        if (success)
        {
            _connectionIdsTlsClientHelloCallbackInvokedMap[request.UConnectionId] = true;
        }
    }

    internal void ConnectionClosed(ulong connectionId)
    {
        _connectionIdsTlsClientHelloCallbackInvokedMap.TryRemove(connectionId, out _);
    }
}
