// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNetCore.HttpOverrides.Internal
{
    public static class IPEndPointParser
    {
        public static bool TryParse(string addressWithPort, out IPEndPoint endpoint)
        {
            string addressPart = null;
            string portPart = null;
            IPAddress address;
            endpoint = null;

            if (string.IsNullOrEmpty(addressWithPort))
            {
                return false;
            }

            var lastColonIndex = addressWithPort.LastIndexOf(':');
            if (lastColonIndex > 0)
            {
                // IPv4 with port or IPv6
                var closingIndex = addressWithPort.LastIndexOf(']');
                if (closingIndex > 0)
                {
                    // IPv6 with brackets
                    addressPart = addressWithPort.Substring(1, closingIndex - 1);
                    if (closingIndex < lastColonIndex)
                    {
                        // IPv6 with port [::1]:80
                        portPart = addressWithPort.Substring(lastColonIndex + 1);
                    }
                }
                else
                {
                    // IPv6 without port or IPv4
                    var firstColonIndex = addressWithPort.IndexOf(':');
                    if (firstColonIndex != lastColonIndex)
                    {
                        // IPv6 ::1
                        addressPart = addressWithPort;
                    }
                    else
                    {
                        // IPv4 with port 127.0.0.1:123
                        addressPart = addressWithPort.Substring(0, firstColonIndex);
                        portPart = addressWithPort.Substring(firstColonIndex + 1);
                    }
                }
            }
            else
            {
                // IPv4 without port
                addressPart = addressWithPort;
            }

            if (IPAddress.TryParse(addressPart, out address))
            {
                if (portPart != null)
                {
                    int port;
                    if (int.TryParse(portPart, out port))
                    {
                        endpoint = new IPEndPoint(address, port);
                        return true;
                    }
                    return false;
                }
                endpoint = new IPEndPoint(address, 0);
                return true;
            }
            return false;
        }
    }
}