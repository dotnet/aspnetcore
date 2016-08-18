// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelServerLimits
    {
        // Matches the non-configurable default response buffer size for Kestrel in 1.0.0
        private long? _maxResponseBufferSize = 64 * 1024;

        // Matches the default client_max_body_size in nginx.  Also large enough that most requests
        // should be under the limit.
        private long? _maxRequestBufferSize = 1024 * 1024;

        // Matches the default large_client_header_buffers in nginx.
        private int _maxRequestLineSize = 8 * 1024;

        // Matches the default large_client_header_buffers in nginx.
        private int _maxRequestHeadersTotalSize = 32 * 1024;

        // Matches the default LimitRequestFields in Apache httpd.
        private int _maxRequestHeaderCount = 100;

        // Matches the default http.sys keep-alive timouet.
        private TimeSpan _keepAliveTimeout = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets the maximum size of the response buffer before write
        /// calls begin to block or return tasks that don't complete until the
        /// buffer size drops below the configured limit.
        /// </summary>
        /// <remarks>
        /// When set to null, the size of the response buffer is unlimited.
        /// When set to zero, all write calls will block or return tasks that
        /// don't complete until the entire response buffer is flushed.
        /// Defaults to 65,536 bytes (64 KB).
        /// </remarks>
        public long? MaxResponseBufferSize
        {
            get
            {
                return _maxResponseBufferSize;
            }
            set
            {
                if (value.HasValue && value.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be null or a non-negative integer.");
                }
                _maxResponseBufferSize = value;
            }
        }

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

        /// <summary>
        /// Gets or sets the maximum allowed size for the HTTP request headers.
        /// </summary>
        /// <remarks>
        /// Defaults to 32,768 bytes (32 KB).
        /// </remarks>
        public int MaxRequestHeadersTotalSize
        {
            get
            {
                return _maxRequestHeadersTotalSize;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must a positive integer.");
                }
                _maxRequestHeadersTotalSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed number of headers per HTTP request.
        /// </summary>
        /// <remarks>
        /// Defaults to 100.
        /// </remarks>
        public int MaxRequestHeaderCount
        {
            get
            {
                return _maxRequestHeaderCount;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must a positive integer.");
                }
                _maxRequestHeaderCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the keep-alive timeout.
        /// </summary>
        /// <remarks>
        /// Defaults to 2 minutes. Timeout granularity is in seconds. Sub-second values will be rounded to the next second.
        /// </remarks>
        public TimeSpan KeepAliveTimeout
        {
            get
            {
                return _keepAliveTimeout;
            }
            set
            {
                _keepAliveTimeout = TimeSpan.FromSeconds(Math.Ceiling(value.TotalSeconds));
            }
        }
    }
}
