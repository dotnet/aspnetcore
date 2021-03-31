// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.AspNetCore.HttpOverrides
{
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
        public IPNetwork(IPAddress prefix, int prefixLength)
        {
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
                if (PrefixBytes[i] != (addressBytes[i] & Mask[i]))
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
    }
}
