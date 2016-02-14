// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class TestConnection
    {
        public static Socket CreateConnectedLoopbackSocket(int port)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (PlatformApis.IsWindows)
            {
                const int SIO_LOOPBACK_FAST_PATH = -1744830448;
                var optionInValue = BitConverter.GetBytes(1);
                try
                {
                    socket.IOControl(SIO_LOOPBACK_FAST_PATH, optionInValue, null);
                }
                catch
                {
                    // If the operating system version on this machine did
                    // not support SIO_LOOPBACK_FAST_PATH (i.e. version
                    // prior to Windows 8 / Windows Server 2012), handle the exception
                }
            }
            socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
            return socket;
        }
    }
}
