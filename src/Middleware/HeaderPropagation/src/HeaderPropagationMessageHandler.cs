// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A message handler for propagating headers collected by the <see cref="HeaderPropagationMiddleware"/> to a outgoing request.
    /// </summary>
    public class HeaderPropagationMessageHandler : DelegatingHandler
    {
        private readonly HeaderPropagationValues _values;
        private readonly HeaderPropagationOptions _options;

        /// <summary>
        /// Creates a new instance of the <see cref="HeaderPropagationMessageHandler"/>.
        /// </summary>
        /// <param name="options">The options that define which headers are propagated.</param>
        /// <param name="values">The values of the headers to be propagated populated by the
        /// <see cref="HeaderPropagationMiddleware"/>.</param>
        public HeaderPropagationMessageHandler(IOptions<HeaderPropagationOptions> options, HeaderPropagationValues values)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;

            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation, after adding
        /// the propagated headers.
        /// </summary>
        /// <remarks>
        /// If an header with the same name is already present in the request, even if empty, the corresponding
        /// propagated header will not be added.
        /// </remarks>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            foreach ((var headerName, var entry) in _options.Headers)
            {
                var outputName = string.IsNullOrEmpty(entry?.OutboundHeaderName) ? headerName : entry.OutboundHeaderName;

                if (!request.Headers.Contains(outputName) &&
                    _values.Headers.TryGetValue(headerName, out var values) &&
                    !StringValues.IsNullOrEmpty(values))
                {
                    request.Headers.TryAddWithoutValidation(outputName, (string[])values);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
