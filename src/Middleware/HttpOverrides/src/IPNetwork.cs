// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.AspNetCore.HttpOverrides;

/// <summary>
/// A representation of an IP network based on CIDR notation.
/// </summary>
public class IPNetwork
{
    /// <summary>
    /// Create a new <see cref="IPNetwork"/> with the specified <see cref="IPAddress"/> and prefix length.
    /// </summary>
    /// <param name="prefix">The <see cref="IPAddress"/>.</param>
    /// <param name="prefixLength">The prefix length.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="prefixLength"/> is out of range.</exception>
    public IPNetwork(IPAddress prefix, int prefixLength) : this(prefix, prefixLength, true)
    {
    }

    private IPNetwork(IPAddress prefix, int prefixLength, bool checkPrefixLengthRange)
    {
        if (checkPrefixLengthRange &&
            !IsValidPrefixLengthRange(prefix, prefixLength))
        {
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "The prefix length was out of range.");
        }

        Prefix = prefix;
        PrefixLength = prefixLength;
        PrefixBytes = Prefix.GetAddressBytes();
        Mask = CreateMask();
    }

    /// <summary>
    /// Get the <see cref="IPAddress"/> that represents the prefix for the network.
    /// </summary>
    public IPAddress Prefix { get; }

    private byte[] PrefixBytes { get; }

    /// <summary>
    /// The CIDR notation of the subnet mask
    /// </summary>
    public int PrefixLength { get; }

    private byte[] Mask { get; }

    /// <summary>
    /// Determine whether a given The <see cref="IPAddress"/> is part of the IP network.
    /// </summary>
    /// <param name="address">The <see cref="IPAddress"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="IPAddress"/> is part of the IP network. Otherwise, <see langword="false"/>.</returns>
    public bool Contains(IPAddress address)
    {
        if (Prefix.AddressFamily != address.AddressFamily)
        {
            return false;
        }

        var addressBytes = address.GetAddressBytes();
        for (int i = 0; i < PrefixBytes.Length && Mask[i] != 0; i++)
        {
            if ((PrefixBytes[i] & Mask[i]) != (addressBytes[i] & Mask[i]))
            {
                return false;
            }
        }

        return true;
    }

    private byte[] CreateMask()
    {
        var mask = new byte[PrefixBytes.Length];
        int remainingBits = PrefixLength;
        int i = 0;
        while (remainingBits >= 8)
        {
            mask[i] = 0xFF;
            i++;
            remainingBits -= 8;
        }
        if (remainingBits > 0)
        {
            mask[i] = (byte)(0xFF << (8 - remainingBits));
        }

        return mask;
    }

    private static bool IsValidPrefixLengthRange(IPAddress prefix, int prefixLength)
    {
        if (prefixLength < 0)
        {
            return false;
        }

        return prefix.AddressFamily switch
        {
            AddressFamily.InterNetwork => prefixLength <= 32,
            AddressFamily.InterNetworkV6 => prefixLength <= 128,
            _ => true
        };
    }

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
        if (!TryParseComponents(networkSpan, out var prefix, out var prefixLength))
        {
            throw new FormatException("An invalid IP address or prefix length was specified.");
        }

        if (!IsValidPrefixLengthRange(prefix, prefixLength))
        {
            throw new ArgumentOutOfRangeException(nameof(networkSpan), "The prefix length was out of range.");
        }

        return new IPNetwork(prefix, prefixLength, false);
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
        network = null;

        if (!TryParseComponents(networkSpan, out var prefix, out var prefixLength))
        {
            return false;
        }

        if (!IsValidPrefixLengthRange(prefix, prefixLength))
        {
            return false;
        }

        network = new IPNetwork(prefix, prefixLength, false);
        return true;
    }

    /// <remarks>
    /// <para>
    /// The specified representation must be expressed using CIDR (Classless Inter-Domain Routing) notation, or 'slash notation',
    /// which contains an IPv4 or IPv6 address and the subnet mask prefix length, separated by a forward slash.
    /// </para>
    /// <example>
    /// e.g. <c>"192.168.0.1/31"</c> for IPv4, <c>"2001:db8:3c4d::1/127"</c> for IPv6
    /// </example>
    /// </remarks>
    private static bool TryParseComponents(
        ReadOnlySpan<char> networkSpan,
        [NotNullWhen(true)] out IPAddress? prefix,
        out int prefixLength)
    {
        prefix = null;
        prefixLength = default;

        var forwardSlashIndex = networkSpan.IndexOf('/');
        if (forwardSlashIndex < 0)
        {
            return false;
        }

        if (!IPAddress.TryParse(networkSpan.Slice(0, forwardSlashIndex), out prefix))
        {
            return false;
        }

        if (!int.TryParse(networkSpan.Slice(forwardSlashIndex + 1), out prefixLength))
        {
            return false;
        }

        return true;
    }
}
