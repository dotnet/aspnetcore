// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IPv6SupportedConditionAttribute : Attribute, ITestCondition
{
    private static readonly Lazy<bool> _ipv6Supported = new Lazy<bool>(CanBindToIPv6Address);

    public bool IsMet => _ipv6Supported.Value;

    public string SkipReason => "IPv6 not supported on the host.";

    private static bool CanBindToIPv6Address()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                return true;
            }
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
