// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    /// <summary>
    /// The maximum we will decode is Int32.MaxValue, which is also the maximum request header field size.
    /// </summary>
    public class IntegerDecoder
    {
        private int _i;
        private int _m;

        /// <summary>
        /// Callers must ensure higher bits above the prefix are cleared before calling this method.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="prefixLength"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool BeginTryDecode(byte b, int prefixLength, out int result)
        {
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

        public bool TryDecode(byte b, out int result)
        {
            var m = _m; // Enregister
            var i = _i + ((b & 0x7f) << m); // Enregister

            if ((b & 0x80) == 0)
            {
                // Int32.MaxValue only needs a maximum of 5 bytes to represent and the last byte cannot have any value set larger than 0x7
                if ((m > 21 && b > 0x7) || i < 0)
                {
                    ThrowIntegerTooBigException();
                }

                result = i;
                return true;
            }
            else if (m > 21)
            {
                // Int32.MaxValue only needs a maximum of 5 bytes to represent
                ThrowIntegerTooBigException();
            }

            _m = m + 7;
            _i = i;

            result = 0;
            return false;
        }

        public static void ThrowIntegerTooBigException()
            => throw new HPackDecodingException(CoreStrings.HPackErrorIntegerTooBig);
    }
}
