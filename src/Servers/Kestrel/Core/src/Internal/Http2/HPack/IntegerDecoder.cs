// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public class IntegerDecoder
    {
        private int _i;
        private int _m;

        public int Value { get; private set; }

        public bool BeginDecode(byte b, int prefixLength)
        {
            if (b < ((1 << prefixLength) - 1))
            {
                Value = b;
                return true;
            }
            else
            {
                _i = b;
                _m = 0;
                return false;
            }
        }

        public bool Decode(byte b)
        {
            _i = _i + (b & 127) * (1 << _m);
            _m = _m + 7;

            if ((b & 128) != 128)
            {
                Value = _i;
                return true;
            }

            return false;
        }
    }
}
