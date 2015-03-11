// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection
{
    internal sealed class TimeLimitedDataProtector : ITimeLimitedDataProtector
    {
        internal const string PurposeString = "Microsoft.AspNet.DataProtection.TimeLimitedDataProtector";

        public TimeLimitedDataProtector(IDataProtector innerProtector)
        {
            InnerProtector = innerProtector;
        }

        internal IDataProtector InnerProtector
        {
            get;
            private set;
        }

        public ITimeLimitedDataProtector CreateProtector([NotNull] string purpose)
        {
            return new TimeLimitedDataProtector(InnerProtector.CreateProtector(purpose));
        }

        public byte[] Protect([NotNull] byte[] plaintext)
        {
            return Protect(plaintext, DateTimeOffset.MaxValue);
        }

        public byte[] Protect([NotNull] byte[] plaintext, DateTimeOffset expiration)
        {
            // We prepend the expiration time (as a big-endian 64-bit UTC tick count) to the unprotected data.
            ulong utcTicksExpiration = (ulong)expiration.UtcTicks;

            byte[] plaintextWithHeader = new byte[checked(8 + plaintext.Length)];
            plaintextWithHeader[0] = (byte)(utcTicksExpiration >> 56);
            plaintextWithHeader[1] = (byte)(utcTicksExpiration >> 48);
            plaintextWithHeader[2] = (byte)(utcTicksExpiration >> 40);
            plaintextWithHeader[3] = (byte)(utcTicksExpiration >> 32);
            plaintextWithHeader[4] = (byte)(utcTicksExpiration >> 24);
            plaintextWithHeader[5] = (byte)(utcTicksExpiration >> 16);
            plaintextWithHeader[6] = (byte)(utcTicksExpiration >> 8);
            plaintextWithHeader[7] = (byte)(utcTicksExpiration);
            Buffer.BlockCopy(plaintext, 0, plaintextWithHeader, 8, plaintext.Length);

            return InnerProtector.Protect(plaintextWithHeader);
        }

        public byte[] Unprotect([NotNull] byte[] protectedData)
        {
            DateTimeOffset unused;
            return Unprotect(protectedData, out unused);
        }

        public byte[] Unprotect([NotNull] byte[] protectedData, out DateTimeOffset expiration)
        {
            try
            {
                byte[] plaintextWithHeader = InnerProtector.Unprotect(protectedData);
                CryptoUtil.Assert(plaintextWithHeader.Length >= 8, "No header present.");

                // Read expiration time back out of the payload
                ulong utcTicksExpiration = (((ulong)plaintextWithHeader[0]) << 56)
                    | (((ulong)plaintextWithHeader[1]) << 48)
                    | (((ulong)plaintextWithHeader[2]) << 40)
                    | (((ulong)plaintextWithHeader[3]) << 32)
                    | (((ulong)plaintextWithHeader[4]) << 24)
                    | (((ulong)plaintextWithHeader[5]) << 16)
                    | (((ulong)plaintextWithHeader[6]) << 8)
                    | (ulong)plaintextWithHeader[7];

                // Are we expired?
                DateTime utcNow = DateTime.UtcNow;
                if ((ulong)utcNow.Ticks > utcTicksExpiration)
                {
                    throw Error.TimeLimitedDataProtector_PayloadExpired(utcTicksExpiration);
                }

                byte[] retVal = new byte[plaintextWithHeader.Length - 8];
                Buffer.BlockCopy(plaintextWithHeader, 8, retVal, 0, retVal.Length);

                expiration = new DateTimeOffset((long)utcTicksExpiration, TimeSpan.Zero);
                return retVal;
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // Homogenize all failures to CryptographicException
                throw Error.CryptCommon_GenericError(ex);
            }
        }

        IDataProtector IDataProtectionProvider.CreateProtector([NotNull] string purpose)
        {
            return CreateProtector(purpose);
        }
    }
}
