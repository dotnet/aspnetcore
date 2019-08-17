// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class IPv6ScopeIdPresentConditionAttribute : Attribute, ITestCondition
    {
        private static readonly Lazy<bool> _ipv6ScopeIdPresent = new Lazy<bool>(IPv6ScopeIdAddressPresent);

        public bool IsMet => _ipv6ScopeIdPresent.Value;

        public string SkipReason => "No IPv6 addresses with scope IDs were found on the host.";

        private static bool IPv6ScopeIdAddressPresent()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(iface => iface.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(iface => iface.GetIPProperties().UnicastAddresses)
                    .Any(addressInfo => addressInfo.Address.AddressFamily == AddressFamily.InterNetworkV6 && addressInfo.Address.ScopeId != 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}