// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Limits only applicable to HTTP/3 connections.
    /// </summary>
    public class Http3Limits
    {
        internal const int DefaultMaxRequestHeaderFieldSize = 16 * 1024;

        private int _headerTableSize;
        private int _maxRequestHeaderFieldSize = DefaultMaxRequestHeaderFieldSize;

        /// <summary>
        /// Limits the size of the header compression table, in octets, the QPACK decoder on the server can use.
        /// <para>
        /// Value must be greater than 0, defaults to 0.
        /// </para>
        /// </summary>
        // TODO: Make public https://github.com/dotnet/aspnetcore/issues/26666
        internal int HeaderTableSize
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
        /// Value must be greater than 0, defaults to 2^14 (16,384).
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
