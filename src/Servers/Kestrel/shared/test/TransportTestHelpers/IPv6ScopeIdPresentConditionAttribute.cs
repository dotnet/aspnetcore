// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

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
