// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.HttpSys.Internal;
using Windows.Win32.Networking.WinSock;
using Xunit;
using static Microsoft.AspNetCore.HttpSys.Internal.SocketAddress;
using SocketAddress = Microsoft.AspNetCore.HttpSys.Internal.SocketAddress;

namespace Microsoft.AspNetCore.Server.HttpSys.Tests.NativeInterop;

public class SocketAddressTests
{
    [Theory]
    [InlineData(80)]
    [InlineData(443)]
    [InlineData(8080)]
    [InlineData(32767)]  // max signed short
    [InlineData(32768)]  // min value that causes negative when cast to short
    [InlineData(42000)]
    [InlineData(65535)]  // Max port number
    public void IPv6_GetPort_ReturnsCorrectPort_ForAllValidPorts(ushort expectedPort)
    {
        var nativeIpV6Address = new SOCKADDR_IN6
        {
            sin6_family = ADDRESS_FAMILY.AF_INET6,
            sin6_port = (ushort)IPAddress.HostToNetworkOrder((short)expectedPort)
        };

        var socketAddress = new SocketAddressIPv6(nativeIpV6Address);
        var actualPort = socketAddress.GetPort();

        Assert.Equal(expectedPort, actualPort);
        Assert.True(actualPort >= 0, "Port should never be negative");
        Assert.True(actualPort <= 65535, "Port should not exceed maximum valid port");
    }

    [Theory]
    [InlineData(80)]
    [InlineData(443)]
    [InlineData(8080)]
    [InlineData(42000)]
    [InlineData(32767)]
    [InlineData(32768)]
    [InlineData(65535)]
    public void IPv4_GetPort_ReturnsCorrectPort_ForAllValidPorts(ushort expectedPort)
    {
        var nativeIpV4Address = new SOCKADDR_IN
        {
            sin_family = ADDRESS_FAMILY.AF_INET,
            sin_port = (ushort)IPAddress.HostToNetworkOrder((short)expectedPort)
        };

        var socketAddress = new SocketAddressIPv4(nativeIpV4Address);
        var actualPort = socketAddress.GetPort();

        Assert.Equal(expectedPort, actualPort);
        Assert.True(actualPort >= 0, "Port should never be negative");
        Assert.True(actualPort <= 65535, "Port should not exceed maximum valid port");
    }
}
