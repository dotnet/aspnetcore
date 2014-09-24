// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNet.Identity
{
    internal sealed class SecurityToken
    {
        private readonly byte[] _data;

        public SecurityToken(byte[] data)
        {
            _data = (byte[])data.Clone();
        }

        internal byte[] GetDataNoClone()
        {
            return _data;
        }
    }

    internal static class Rfc6238AuthenticationService
    {
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan _timestep = TimeSpan.FromMinutes(3);
        private static readonly Encoding _encoding = new UTF8Encoding(false, true);

        private static int ComputeTotp(HashAlgorithm hashAlgorithm, ulong timestepNumber, string modifier)
        {
            // # of 0's = length of pin
            const int Mod = 1000000;

            // See https://tools.ietf.org/html/rfc4226
            // We can add an optional modifier
            var timestepAsBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)timestepNumber));
            var hash = hashAlgorithm.ComputeHash(ApplyModifier(timestepAsBytes, modifier));

            // Generate DT string
            var offset = hash[hash.Length - 1] & 0xf;
            Debug.Assert(offset + 4 < hash.Length);
            var binaryCode = (hash[offset] & 0x7f) << 24
                             | (hash[offset + 1] & 0xff) << 16
                             | (hash[offset + 2] & 0xff) << 8
                             | (hash[offset + 3] & 0xff);

            return binaryCode % Mod;
        }

        private static byte[] ApplyModifier(byte[] input, string modifier)
        {
            if (String.IsNullOrEmpty(modifier))
            {
                return input;
            }

            var modifierBytes = _encoding.GetBytes(modifier);
            var combined = new byte[checked(input.Length + modifierBytes.Length)];
            Buffer.BlockCopy(input, 0, combined, 0, input.Length);
            Buffer.BlockCopy(modifierBytes, 0, combined, input.Length, modifierBytes.Length);
            return combined;
        }

        // More info: https://tools.ietf.org/html/rfc6238#section-4
        private static ulong GetCurrentTimeStepNumber()
        {
            var delta = DateTime.UtcNow - _unixEpoch;
            return (ulong)(delta.Ticks / _timestep.Ticks);
        }

        public static int GenerateCode(SecurityToken securityToken, string modifier = null)
        {
            if (securityToken == null)
            {
                throw new ArgumentNullException("securityToken");
            }

            // Allow a variance of no greater than 90 seconds in either direction
            var currentTimeStep = GetCurrentTimeStepNumber();
            using (var hashAlgorithm = new HMACSHA1(securityToken.GetDataNoClone()))
            {
                return ComputeTotp(hashAlgorithm, currentTimeStep, modifier);
            }
        }

        public static bool ValidateCode(SecurityToken securityToken, int code, string modifier = null)
        {
            if (securityToken == null)
            {
                throw new ArgumentNullException("securityToken");
            }

            // Allow a variance of no greater than 90 seconds in either direction
            var currentTimeStep = GetCurrentTimeStepNumber();
            using (var hashAlgorithm = new HMACSHA1(securityToken.GetDataNoClone()))
            {
                for (var i = -2; i <= 2; i++)
                {
                    var computedTotp = ComputeTotp(hashAlgorithm, (ulong)((long)currentTimeStep + i), modifier);
                    if (computedTotp == code)
                    {
                        return true;
                    }
                }
            }

            // No match
            return false;
        }
    }
}
