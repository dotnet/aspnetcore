// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using Microsoft.AspNet.Cryptography;

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

        public byte[] Protect([NotNull] byte[] unprotectedData)
        {
            return Protect(unprotectedData, DateTimeOffset.MaxValue);
        }

        public byte[] Protect([NotNull] byte[] unprotectedData, DateTimeOffset expiration)
        {
            // We prepend the expiration time (as a big-endian 64-bit UTC tick count) to the unprotected data.
            ulong utcTicksExpiration = (ulong)expiration.UtcTicks;

            byte[] unprotectedDataWithHeader = new byte[checked(8 + unprotectedData.Length)];
            unprotectedDataWithHeader[0] = (byte)(utcTicksExpiration >> 56);
            unprotectedDataWithHeader[1] = (byte)(utcTicksExpiration >> 48);
            unprotectedDataWithHeader[2] = (byte)(utcTicksExpiration >> 40);
            unprotectedDataWithHeader[3] = (byte)(utcTicksExpiration >> 32);
            unprotectedDataWithHeader[4] = (byte)(utcTicksExpiration >> 24);
            unprotectedDataWithHeader[5] = (byte)(utcTicksExpiration >> 16);
            unprotectedDataWithHeader[6] = (byte)(utcTicksExpiration >> 8);
            unprotectedDataWithHeader[7] = (byte)(utcTicksExpiration);
            Buffer.BlockCopy(unprotectedData, 0, unprotectedDataWithHeader, 8, unprotectedData.Length);

            return InnerProtector.Protect(unprotectedDataWithHeader);
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
                byte[] unprotectedDataWithHeader = InnerProtector.Unprotect(protectedData);
                CryptoUtil.Assert(unprotectedDataWithHeader.Length >= 8, "No header present.");

                // Read expiration time back out of the payload
                ulong utcTicksExpiration = (((ulong)unprotectedDataWithHeader[0]) << 56)
                    | (((ulong)unprotectedDataWithHeader[1]) << 48)
                    | (((ulong)unprotectedDataWithHeader[2]) << 40)
                    | (((ulong)unprotectedDataWithHeader[3]) << 32)
                    | (((ulong)unprotectedDataWithHeader[4]) << 24)
                    | (((ulong)unprotectedDataWithHeader[5]) << 16)
                    | (((ulong)unprotectedDataWithHeader[6]) << 8)
                    | (ulong)unprotectedDataWithHeader[7];

                // Are we expired?
                DateTime utcNow = DateTime.UtcNow;
                if ((ulong)utcNow.Ticks > utcTicksExpiration)
                {
                    throw Error.TimeLimitedDataProtector_PayloadExpired(utcTicksExpiration);
                }

                byte[] retVal = new byte[unprotectedDataWithHeader.Length - 8];
                Buffer.BlockCopy(unprotectedDataWithHeader, 8, retVal, 0, retVal.Length);

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
