// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.AspNetCore.DataProtection.Extensions;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Wraps an existing <see cref="IDataProtector"/> and appends a purpose that allows
    /// protecting data with a finite lifetime.
    /// </summary>
    internal sealed class TimeLimitedDataProtector : ITimeLimitedDataProtector
    {
        private const string MyPurposeString = "Microsoft.AspNetCore.DataProtection.TimeLimitedDataProtector.v1";

        private readonly IDataProtector _innerProtector;
        private IDataProtector _innerProtectorWithTimeLimitedPurpose; // created on-demand

        public TimeLimitedDataProtector(IDataProtector innerProtector)
        {
            _innerProtector = innerProtector;
        }

        public ITimeLimitedDataProtector CreateProtector(string purpose)
        {
            if (purpose == null)
            {
                throw new ArgumentNullException(nameof(purpose));
            }

            return new TimeLimitedDataProtector(_innerProtector.CreateProtector(purpose));
        }

        private IDataProtector GetInnerProtectorWithTimeLimitedPurpose()
        {
            // thread-safe lazy init pattern with multi-execution and single publication
            var retVal = Volatile.Read(ref _innerProtectorWithTimeLimitedPurpose);
            if (retVal == null)
            {
                var newValue = _innerProtector.CreateProtector(MyPurposeString); // we always append our purpose to the end of the chain
                retVal = Interlocked.CompareExchange(ref _innerProtectorWithTimeLimitedPurpose, newValue, null) ?? newValue;
            }
            return retVal;
        }

        public byte[] Protect(byte[] plaintext, DateTimeOffset expiration)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            // We prepend the expiration time (as a 64-bit UTC tick count) to the unprotected data.
            byte[] plaintextWithHeader = new byte[checked(8 + plaintext.Length)];
            BitHelpers.WriteUInt64(plaintextWithHeader, 0, (ulong)expiration.UtcTicks);
            Buffer.BlockCopy(plaintext, 0, plaintextWithHeader, 8, plaintext.Length);

            return GetInnerProtectorWithTimeLimitedPurpose().Protect(plaintextWithHeader);
        }

        public byte[] Unprotect(byte[] protectedData, out DateTimeOffset expiration)
        {
            if (protectedData == null)
            {
                throw new ArgumentNullException(nameof(protectedData));
            }

            return UnprotectCore(protectedData, DateTimeOffset.UtcNow, out expiration);
        }

        internal byte[] UnprotectCore(byte[] protectedData, DateTimeOffset now, out DateTimeOffset expiration)
        {
            if (protectedData == null)
            {
                throw new ArgumentNullException(nameof(protectedData));
            }

            try
            {
                byte[] plaintextWithHeader = GetInnerProtectorWithTimeLimitedPurpose().Unprotect(protectedData);
                if (plaintextWithHeader.Length < 8)
                {
                    // header isn't present
                    throw new CryptographicException(Resources.TimeLimitedDataProtector_PayloadInvalid);
                }

                // Read expiration time back out of the payload
                ulong utcTicksExpiration = BitHelpers.ReadUInt64(plaintextWithHeader, 0);
                DateTimeOffset embeddedExpiration = new DateTimeOffset(checked((long)utcTicksExpiration), TimeSpan.Zero /* UTC */);

                // Are we expired?
                if (now > embeddedExpiration)
                {
                    throw new CryptographicException(Resources.FormatTimeLimitedDataProtector_PayloadExpired(embeddedExpiration));
                }

                // Not expired - split and return payload
                byte[] retVal = new byte[plaintextWithHeader.Length - 8];
                Buffer.BlockCopy(plaintextWithHeader, 8, retVal, 0, retVal.Length);
                expiration = embeddedExpiration;
                return retVal;
            }
            catch (Exception ex) when (ex.RequiresHomogenization())
            {
                // Homogenize all failures to CryptographicException
                throw new CryptographicException(Resources.CryptCommon_GenericError, ex);
            }
        }

        /*
         * EXPLICIT INTERFACE IMPLEMENTATIONS
         */

        IDataProtector IDataProtectionProvider.CreateProtector(string purpose)
        {
            if (purpose == null)
            {
                throw new ArgumentNullException(nameof(purpose));
            }

            return CreateProtector(purpose);
        }

        byte[] IDataProtector.Protect(byte[] plaintext)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException(nameof(plaintext));
            }

            // MaxValue essentially means 'no expiration'
            return Protect(plaintext, DateTimeOffset.MaxValue);
        }

        byte[] IDataProtector.Unprotect(byte[] protectedData)
        {
            if (protectedData == null)
            {
                throw new ArgumentNullException(nameof(protectedData));
            }

            DateTimeOffset expiration; // unused
            return Unprotect(protectedData, out expiration);
        }
    }
}
