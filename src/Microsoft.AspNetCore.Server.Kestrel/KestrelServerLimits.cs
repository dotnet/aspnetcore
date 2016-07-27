// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelServerLimits
    {
        // Matches the default client_max_body_size in nginx.  Also large enough that most requests
        // should be under the limit.
        private long? _maxRequestBufferSize = 1024 * 1024;

        // Matches the default large_client_header_buffers in nginx.
        private int _maxRequestLineSize = 8 * 1024;

        /// <summary>
        /// Gets or sets the maximum size of the request buffer.
        /// </summary>
        /// <remarks>
        /// When set to null, the size of the request buffer is unlimited.
        /// Defaults to 1,048,576 bytes (1 MB).
        /// </remarks>
        public long? MaxRequestBufferSize
        {
            get
            {
                return _maxRequestBufferSize;
            }
            set
            {
                if (value.HasValue && value.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be null or a positive integer.");
                }
                _maxRequestBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed size for the HTTP request line.
        /// </summary>
        /// <remarks>
        /// Defaults to 8,192 bytes (8 KB).
        /// </remarks>
        public int MaxRequestLineSize
        {
            get
            {
                return _maxRequestLineSize;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be a positive integer.");
                }
                _maxRequestLineSize = value;
            }
        }
    }
}
