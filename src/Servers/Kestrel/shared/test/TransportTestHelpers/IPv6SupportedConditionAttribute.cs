// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
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
}
