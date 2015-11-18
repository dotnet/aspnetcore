// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNet.IISPlatformHandler
{
    public static class IPAddressWithPortParser
    {
        public static bool TryParse(string addressWithPort, out IPAddress address, out int? port)
        {
            port = null;

            string addressPart = null;
            string portPart = null;

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

            var success = IPAddress.TryParse(addressPart, out address);
            if (success && portPart != null)
            {
                int portValue;
                success &= int.TryParse(portPart, out portValue);
                if (success)
                {
                    port = portValue;
                }
                else
                {
                    // we cannot parse port, reset address
                    address = null;
                }
            }
            return success;
        }
    }
}
