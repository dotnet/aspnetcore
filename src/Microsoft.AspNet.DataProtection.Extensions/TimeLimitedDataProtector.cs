// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.AspNet.DataProtection.Extensions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// Wraps an existing <see cref="IDataProtector"/> and appends a purpose that allows
    /// protecting data with a finite lifetime.
    /// </summary>
    internal sealed class TimeLimitedDataProtector : ITimeLimitedDataProtector
    {
        private const string MyPurposeString = "Microsoft.AspNet.DataProtection.TimeLimitedDataProtector.v1";

        private readonly IDataProtector _innerProtector;
        private IDataProtector _innerProtectorWithTimeLimitedPurpose; // created on-demand

        public TimeLimitedDataProtector(IDataProtector innerProtector)
        {
            _innerProtector = innerProtector;
        }

        public ITimeLimitedDataProtector CreateProtector([NotNull] string purpose)
        {
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

        public byte[] Protect([NotNull] byte[] plaintext, DateTimeOffset expiration)
        {
            // We prepend the expiration time (as a 64-bit UTC tick count) to the unprotected data.
            byte[] plaintextWithHeader = new byte[checked(8 + plaintext.Length)];
            BitHelpers.WriteUInt64(plaintextWithHeader, 0, (ulong)expiration.UtcTicks);
            Buffer.BlockCopy(plaintext, 0, plaintextWithHeader, 8, plaintext.Length);

            return GetInnerProtectorWithTimeLimitedPurpose().Protect(plaintextWithHeader);
        }

        public byte[] Unprotect([NotNull] byte[] protectedData, out DateTimeOffset expiration)
        {
            return UnprotectCore(protectedData, DateTimeOffset.UtcNow, out expiration);
        }

        internal byte[] UnprotectCore([NotNull] byte[] protectedData, DateTimeOffset now, out DateTimeOffset expiration)
        {
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
                expiration = new DateTimeOffset((long)utcTicksExpiration, TimeSpan.Zero);
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
            return CreateProtector(purpose);
        }

        byte[] IDataProtector.Protect(byte[] plaintext)
        {
            // MaxValue essentially means 'no expiration'
            return Protect(plaintext, DateTimeOffset.MaxValue);
        }

        byte[] IDataProtector.Unprotect(byte[] protectedData)
        {
            DateTimeOffset expiration; // unused
            return Unprotect(protectedData, out expiration);
        }
    }
}
