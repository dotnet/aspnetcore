// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class HttpSysOptions
    {
        private const long DefaultRequestQueueLength = 1000; // Http.sys default.
        internal static readonly int DefaultMaxAccepts = 5 * Environment.ProcessorCount;

        // The native request queue
        private long _requestQueueLength = DefaultRequestQueueLength;
        private RequestQueue _requestQueue;

        public HttpSysOptions()
        {
        }

        /// <summary>
        /// The maximum number of concurrent accepts.
        /// </summary>
        public int MaxAccepts { get; set; } = DefaultMaxAccepts;

        /// <summary>
        /// Attempts kernel mode caching for responses with eligible headers. The response may not include
        /// Set-Cookie, Vary, or Pragma headers. It must include a Cache-Control header with Public and
        /// either a Shared-Max-Age or Max-Age value, or an Expires header.
        /// </summary>
        public bool EnableResponseCaching { get; set; } = true;

        /// <summary>
        /// The url prefixes to register with Http.Sys. These may be modified at any time prior to disposing
        /// the listener.
        /// </summary>
        public UrlPrefixCollection UrlPrefixes { get; } = new UrlPrefixCollection();

        /// <summary>
        /// Http.Sys authentication settings. These may be modified at any time prior to disposing
        /// the listener.
        /// </summary>
        public AuthenticationManager Authentication { get; } = new AuthenticationManager();

        /// <summary>
        /// Exposes the Http.Sys timeout configurations.  These may also be configured in the registry.
        /// These may be modified at any time prior to disposing the listener.
        /// </summary>
        public TimeoutManager Timeouts { get; } = new TimeoutManager();

        /// <summary>
        /// Gets or Sets if response body writes that fail due to client disconnects should throw exceptions or
        /// complete normally. The default is false.
        /// </summary>
        public bool ThrowWriteExceptions { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of requests that will be queued up in Http.Sys.
        /// </summary>
        public long RequestQueueLimit
        {
            get
            {
                return _requestQueueLength;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Empty);
                }

                if (_requestQueue != null)
                {
                    _requestQueue.SetLengthLimit(_requestQueueLength);
                }
                // Only store it if it succeeds or hasn't started yet
                _requestQueueLength = value;
            }
        }

        /// <summary>
        /// The amount of time to wait for active requests to drain while the server is shutting down.
        /// New requests will receive a 503 response in this time period. The default is 5 seconds.
        /// </summary>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

        internal void SetRequestQueueLimit(RequestQueue requestQueue)
        {
            _requestQueue = requestQueue;
            if (_requestQueueLength != DefaultRequestQueueLength)
            {
                _requestQueue.SetLengthLimit(_requestQueueLength);
            }
        }
    }
}
