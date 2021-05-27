// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Net.Http.HPack
{
    internal sealed class IntegerDecoder
    {
        private int _i;
        private int _m;

        /// <summary>
        /// Decodes the first byte of the integer.
        /// </summary>
        /// <param name="b">
        /// The first byte of the variable-length encoded integer.
        /// </param>
        /// <param name="prefixLength">
        /// The number of lower bits in this prefix byte that the
        /// integer has been encoded into. Must be between 1 and 8.
        /// Upper bits must be zero.
        /// </param>
        /// <param name="result">
        /// If decoded successfully, contains the decoded integer.
        /// </param>
        /// <returns>
        /// If the integer has been fully decoded, true.
        /// Otherwise, false -- <see cref="TryDecode(byte, out int)"/> must be called on subsequent bytes.
        /// </returns>
        /// <remarks>
        /// The term "prefix" can be confusing. From the HPACK spec:
        /// An integer is represented in two parts: a prefix that fills the current octet and an
        /// optional list of octets that are used if the integer value does not fit within the prefix.
        /// </remarks>
        public bool BeginTryDecode(byte b, int prefixLength, out int result)
        {
            Debug.Assert(prefixLength >= 1 && prefixLength <= 8);
            Debug.Assert((b & ~((1 << prefixLength) - 1)) == 0, "bits other than prefix data must be set to 0.");

            if (b < ((1 << prefixLength) - 1))
            {
                result = b;
                return true;
            }

            _i = b;
            _m = 0;
            result = 0;
            return false;
        }

        /// <summary>
        /// Decodes subsequent bytes of an integer.
        /// </summary>
        /// <param name="b">The next byte.</param>
        /// <param name="result">
        /// If decoded successfully, contains the decoded integer.
        /// </param>
        /// <returns>If the integer has been fully decoded, true. Otherwise, false -- <see cref="TryDecode(byte, out int)"/> must be called on subsequent bytes.</returns>
        public bool TryDecode(byte b, out int result)
        {
            // Check if shifting b by _m would result in > 31 bits.
            // No masking is required: if the 8th bit is set, it indicates there is a
            // bit set in a future byte, so it is fine to check that here as if it were
            // bit 0 on the next byte.
            // This is a simplified form of:
            //   int additionalBitsRequired = 32 - BitOperations.LeadingZeroCount((uint)b);
            //   if (_m + additionalBitsRequired > 31)
            if (BitOperations.LeadingZeroCount((uint)b) <= _m)
            {
                throw new HPackDecodingException(SR.net_http_hpack_bad_integer);
            }

            _i = _i + ((b & 0x7f) << _m);

            // If the addition overflowed, the result will be negative.
            if (_i < 0)
            {
                throw new HPackDecodingException(SR.net_http_hpack_bad_integer);
            }

            _m = _m + 7;

            if ((b & 128) == 0)
            {
                if (b == 0 && _m / 7 > 1)
                {
                    // Do not accept overlong encodings.
                    throw new HPackDecodingException(SR.net_http_hpack_bad_integer);
                }

                result = _i;
                return true;
            }

            result = 0;
            return false;
        }
    }
}
