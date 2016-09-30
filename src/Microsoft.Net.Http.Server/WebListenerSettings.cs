// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
{
    public class WebListenerSettings
    {
        private const long DefaultRequestQueueLength = 1000; // Http.sys default.

        // The native request queue
        private long _requestQueueLength = DefaultRequestQueueLength;
        private RequestQueue _requestQueue;
        private ILogger _logger = NullLogger.Instance;

        public WebListenerSettings()
        {
        }

        /// <summary>
        /// The logger that will be used to create the WebListener instance. This should not be changed
        /// after creating the listener.
        /// </summary>
        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _logger = value;
            }
        }

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
