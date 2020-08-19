// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public class Http3Limits
    {
        private int _headerTableSize = 4096;
        private int _maxRequestHeaderFieldSize = 8192;

        /// <summary>
        /// Limits the size of the header compression table, in octets, the HPACK decoder on the server can use.
        /// <para>
        /// Value must be greater than 0, defaults to 4096
        /// </para>
        /// </summary>
        public int HeaderTableSize
        {
            get => _headerTableSize;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
                }

                _headerTableSize = value;
            }
        }

        /// <summary>
        /// Indicates the size of the maximum allowed size of a request header field sequence. This limit applies to both name and value sequences in their compressed and uncompressed representations.
        /// <para>
        /// Value must be greater than 0, defaults to 8192
        /// </para>
        /// </summary>
        public int MaxRequestHeaderFieldSize
        {
            get => _maxRequestHeaderFieldSize;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
                }

                _maxRequestHeaderFieldSize = value;
            }
        }
    }
}
