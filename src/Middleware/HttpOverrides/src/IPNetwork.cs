// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Microsoft.AspNetCore.HttpOverrides;

/// <summary>
/// A representation of an IP network based on CIDR notation.
/// </summary>
[System.Obsolete("Please use System.Net.IPNetwork instead")]
public class IPNetwork
{
    private readonly System.Net.IPNetwork _network;

    /// <summary>
    /// Create a new <see cref="IPNetwork"/> with the specified <see cref="IPAddress"/> and prefix length.
    /// </summary>
    /// <param name="prefix">The <see cref="IPAddress"/>.</param>
    /// <param name="prefixLength">The prefix length.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="prefixLength"/> is out of range.</exception>
    public IPNetwork(IPAddress prefix, int prefixLength)
    {
        _network = new(prefix, prefixLength);
    }

    private IPNetwork(System.Net.IPNetwork network) => _network = network;

    /// <summary>
    /// Get the <see cref="IPAddress"/> that represents the prefix for the network.
    /// </summary>
    public IPAddress Prefix => _network.BaseAddress;

    /// <summary>
    /// The CIDR notation of the subnet mask
    /// </summary>
    public int PrefixLength => _network.PrefixLength;

    /// <summary>
    /// Determine whether a given The <see cref="IPAddress"/> is part of the IP network.
    /// </summary>
    /// <param name="address">The <see cref="IPAddress"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="IPAddress"/> is part of the IP network. Otherwise, <see langword="false"/>.</returns>
    public bool Contains(IPAddress address) => _network.Contains(address);

    /// <inheritdoc cref="System.Net.IPNetwork.Parse(ReadOnlySpan{char})"/>
    public static IPNetwork Parse(ReadOnlySpan<char> networkSpan) => System.Net.IPNetwork.Parse(networkSpan);

    /// <inheritdoc cref="System.Net.IPNetwork.TryParse(ReadOnlySpan{char}, out System.Net.IPNetwork)"/>
    public static bool TryParse(ReadOnlySpan<char> networkSpan, [NotNullWhen(true)] out IPNetwork? network)
    {
        if (System.Net.IPNetwork.TryParse(networkSpan, out var ipNetwork))
        {
            network = ipNetwork;
            return true;
        }

        network = null;
        return false;
    }

    /// <summary>
    /// Convert <see cref="System.Net.IPNetwork" /> to <see cref="Microsoft.AspNetCore.HttpOverrides.IPNetwork" /> implicitly
    /// </summary>
    public static implicit operator IPNetwork(System.Net.IPNetwork ipNetwork) => new IPNetwork(ipNetwork);
}
