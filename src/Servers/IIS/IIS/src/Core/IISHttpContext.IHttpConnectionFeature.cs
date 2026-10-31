// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal partial class IISHttpContext : IHttpConnectionFeature
{
    IPAddress? IHttpConnectionFeature.RemoteIpAddress
    {
        get
        {
            if (RemoteIpAddress == null)
            {
                InitializeRemoteEndpoint();
            }

            return RemoteIpAddress;
        }
        set => RemoteIpAddress = value;
    }

    IPAddress? IHttpConnectionFeature.LocalIpAddress
    {
        get
        {
            if (LocalIpAddress == null)
            {
                InitializeLocalEndpoint();
            }
            return LocalIpAddress;
        }
        set => LocalIpAddress = value;
    }

    int IHttpConnectionFeature.RemotePort
    {
        get
        {
            if (RemoteIpAddress == null)
            {
                InitializeRemoteEndpoint();
            }

            return RemotePort;
        }
        set => RemotePort = value;
    }

    int IHttpConnectionFeature.LocalPort
    {
        get
        {
            if (LocalIpAddress == null)
            {
                InitializeLocalEndpoint();
            }

            return LocalPort;
        }
        set => LocalPort = value;
    }

    string IHttpConnectionFeature.ConnectionId
    {
        get
        {
            if (RequestConnectionId == null)
            {
                InitializeConnectionId();
            }

            return RequestConnectionId;
        }
        set => RequestConnectionId = value;
    }

    private void InitializeLocalEndpoint()
    {
        var localEndPoint = GetLocalEndPoint();
        if (localEndPoint != null)
        {
            LocalIpAddress = localEndPoint.GetIPAddress();
            LocalPort = localEndPoint.GetPort();
        }
    }

    private void InitializeRemoteEndpoint()
    {
        var remoteEndPoint = GetRemoteEndPoint();
        if (remoteEndPoint != null)
        {
            RemoteIpAddress = remoteEndPoint.GetIPAddress();
            RemotePort = remoteEndPoint.GetPort();
        }
    }

    [MemberNotNull(nameof(RequestConnectionId))]
    private void InitializeConnectionId()
    {
        RequestConnectionId = ConnectionId.ToString(CultureInfo.InvariantCulture);
    }
}
