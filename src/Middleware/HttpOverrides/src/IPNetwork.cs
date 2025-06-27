// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

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

    /// <summary>
    /// Converts the specified <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> representation of
    /// an IP address and a prefix length to its <see cref="IPNetwork"/> equivalent.
    /// </summary>
    /// <param name="networkSpan">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to convert, in CIDR notation.</param>
    /// <returns>
    ///The <see cref="IPNetwork"/> equivalent to the IP address and prefix length contained in <paramref name="networkSpan"/>.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="networkSpan"/> is not in the correct format.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The prefix length contained in <paramref name="networkSpan"/> is out of range.</exception>
    /// <inheritdoc cref="TryParseComponents(ReadOnlySpan{char}, out IPAddress?, out int)"/>
    public static IPNetwork Parse(ReadOnlySpan<char> networkSpan)
    {
        return System.Net.IPNetwork.Parse(networkSpan);
    }

    /// <summary>
    /// Converts the specified <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> representation of
    /// an IP address and a prefix length to its <see cref="IPNetwork"/> equivalent, and returns a value
    /// that indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="networkSpan">The <see cref="ReadOnlySpan{T}"/> of <see langword="char"/> to validate.</param>
    /// <param name="network">
    /// When this method returns, contains the <see cref="IPNetwork"/> equivalent to the IP Address
    /// and prefix length contained in <paramref name="networkSpan"/>, if the conversion succeeded,
    /// or <see langword="null"/> if the conversion failed. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="networkSpan"/> parameter was
    /// converted successfully; otherwise <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="TryParseComponents(ReadOnlySpan{char}, out IPAddress?, out int)"/>
    public static bool TryParse(ReadOnlySpan<char> networkSpan, [NotNullWhen(true)] out IPNetwork? network)
    {
        if (System.Net.IPNetwork.TryParse(networkSpan, out var ipNetwork))
        {
            network = ipNetwork;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Convert <see cref="System.Net.IPNetwork" /> to <see cref="Microsoft.AspNetCore.HttpOverrides.IPNetwork" /> implicitly
    /// </sumary>
    public static implicit operator IPNetwork(System.Net.IPNetwork ipNetwork)
    {
        return new IPNetwork(ipNetwork);
    }
}
