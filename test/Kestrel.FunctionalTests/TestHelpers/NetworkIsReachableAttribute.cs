// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NetworkIsReachableAttribute : Attribute, ITestCondition
    {
        private string _hostname;
        private string _error;

        public bool IsMet
        {
            get
            {
                try
                {
                    _hostname = Dns.GetHostName();

                    // if the network is unreachable on macOS, throws with SocketError.NetworkUnreachable
                    // if the network device is not configured, throws with SocketError.HostNotFound
                    // if the network is reachable, throws with SocketError.ConnectionRefused or succeeds
                    HttpClientSlim.GetStringAsync($"http://{_hostname}").GetAwaiter().GetResult();
                }
                catch (SocketException ex) when (
                    ex.SocketErrorCode == SocketError.NetworkUnreachable
                    || ex.SocketErrorCode == SocketError.HostNotFound)
                {
                    _error = ex.Message;
                    return false;
                }
                catch
                {
                    // Swallow other errors. Allows the test to throw the failures instead
                }

                return true;
            }
        }

        public string SkipReason => _hostname != null
            ? $"Test cannot run when network is unreachable. Socket exception: '{_error}'"
            : "Could not determine hostname for current test machine";
    }
}
