// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Limits only applicable to HTTP/2 connections.
    /// </summary>
    public class Http2Limits
    {
        private int _maxStreamsPerConnection = 100;
        private int _headerTableSize = MaxAllowedHeaderTableSize;
        private int _maxFrameSize = MinAllowedMaxFrameSize;

        // These are limits defined by the RFC https://tools.ietf.org/html/rfc7540#section-4.2
        public const int MaxAllowedHeaderTableSize = 4096;
        public const int MinAllowedMaxFrameSize = 16 * 1024;
        public const int MaxAllowedMaxFrameSize = 16 * 1024 * 1024 - 1;

        /// <summary>
        /// Limits the number of concurrent request streams per HTTP/2 connection. Excess streams will be refused.
        /// <para>
        /// Defaults to 100
        /// </para>
        /// </summary>
        public int MaxStreamsPerConnection
        {
            get => _maxStreamsPerConnection;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.GreaterThanZeroRequired);
                }

                _maxStreamsPerConnection = value;
            }
        }

        /// <summary>
        /// Limits the size of the header compression table, in octets, the HPACK decoder on the server can use.
        /// <para>
        /// Defaults to 4096
        /// </para>
        /// </summary>
        public int HeaderTableSize
        {
            get => _headerTableSize;
            set
            {
                if (value <= 0 || value > MaxAllowedHeaderTableSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.FormatArgumentOutOfRange(0, MaxAllowedHeaderTableSize));
                }

                _headerTableSize = value;
            }
        }

        /// <summary>
        /// Indicates the size of the largest frame payload that is allowed to be received, in octets. The size must be between 2^14 and 2^24-1.
        /// <para>
        /// Defaults to 2^14 (16,384)
        /// </para>
        /// </summary>
        public int MaxFrameSize
        {
            get => _maxFrameSize;
            set
            {
                if (value < MinAllowedMaxFrameSize || value > MaxAllowedMaxFrameSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.FormatArgumentOutOfRange(MinAllowedMaxFrameSize, MaxAllowedMaxFrameSize));
                }

                _maxFrameSize = value;
            }
        }
    }
}
