// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Summary description for TestConnection
/// </summary>
public class TestConnection : StreamBackedTestConnection
{
    private readonly Socket _socket;

    public TestConnection(int port)
        : this(port, AddressFamily.InterNetwork)
    {
    }

    public TestConnection(int port, AddressFamily addressFamily)
        : this(CreateConnectedLoopbackSocket(port, addressFamily), ownsSocket: true)
    {
    }

    public TestConnection(Socket socket)
        : this(socket, ownsSocket: false)
    {
    }

    private TestConnection(Socket socket, bool ownsSocket)
        : base(new NetworkStream(socket, ownsSocket: ownsSocket))
    {
        _socket = socket;
    }

    public Socket Socket => _socket;

    public void Shutdown(SocketShutdown how)
    {
        _socket.Shutdown(how);
    }

    public override void ShutdownSend()
    {
        Shutdown(SocketShutdown.Send);
    }

    public override void Reset()
    {
        _socket.LingerState = new LingerOption(true, 0);
        _socket.Dispose();
    }

    public static Socket CreateConnectedLoopbackSocket(int port) => CreateConnectedLoopbackSocket(port, AddressFamily.InterNetwork);

    public static Socket CreateConnectedLoopbackSocket(int port, AddressFamily addressFamily)
    {
        if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
        {
            throw new ArgumentException($"TestConnection does not support address family of type {addressFamily}", nameof(addressFamily));
        }

        var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        var address = addressFamily == AddressFamily.InterNetworkV6
            ? IPAddress.IPv6Loopback
            : IPAddress.Loopback;
        socket.Connect(new IPEndPoint(address, port));
        return socket;
    }
}
