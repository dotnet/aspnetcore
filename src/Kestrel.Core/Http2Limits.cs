// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// Limits only applicable to HTTP/2 connections.
    /// </summary>
    public class Http2Limits
    {
        private int _maxStreamsPerConnection = 100;
        private int _headerTableSize = (int)Http2PeerSettings.DefaultHeaderTableSize;
        private int _maxFrameSize = (int)Http2PeerSettings.DefaultMaxFrameSize;
        private int _maxRequestHeaderFieldSize = 8192;

        /// <summary>
        /// Limits the number of concurrent request streams per HTTP/2 connection. Excess streams will be refused.
        /// <para>
        /// Value must be greater than 0, defaults to 100
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
        /// Indicates the size of the largest frame payload that is allowed to be received, in octets. The size must be between 2^14 and 2^24-1.
        /// <para>
        /// Value must be between 2^14 and 2^24, defaults to 2^14 (16,384)
        /// </para>
        /// </summary>
        public int MaxFrameSize
        {
            get => _maxFrameSize;
            set
            {
                if (value < Http2PeerSettings.MinAllowedMaxFrameSize || value > Http2PeerSettings.MaxAllowedMaxFrameSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, CoreStrings.FormatArgumentOutOfRange(Http2PeerSettings.MinAllowedMaxFrameSize, Http2PeerSettings.MaxAllowedMaxFrameSize));
                }

                _maxFrameSize = value;
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
