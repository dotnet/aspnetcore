// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.SpaServices.Util
{
    internal static class TcpPortFinder
    {
        public static int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        public static bool TestPortAvailability(int port)
        {
            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var ipEndPoints = ipProperties.GetActiveTcpListeners();
            foreach (var endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
