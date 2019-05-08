// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
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
            var captured = _values.Headers;
            if (captured == null)
            {
                var message =
                    $"The {nameof(HeaderPropagationValues)}.{nameof(HeaderPropagationValues.Headers)} property has not been " +
                    $"initialized. Register the header propagation middleware by adding 'app.{nameof(HeaderPropagationApplicationBuilderExtensions.UseHeaderPropagation)}() " +
                    $"in the 'Configure(...)' method.";
                throw new InvalidOperationException(message);
            }

            // Perf: We iterate _options.Headers instead of iterating _values.Headers because iterating an IDictionary
            // will allocate. Also avoiding foreach since we don't define a struct-enumerator.
            var entries = _options.Headers;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var hasContent = request.Content != null;

                if (!request.Headers.TryGetValues(entry.OutboundHeaderName, out var _) &&
                    !(hasContent && request.Content.Headers.TryGetValues(entry.OutboundHeaderName, out var _)))
                {
                    if (captured.TryGetValue(entry.OutboundHeaderName, out var stringValues) &&
                        !StringValues.IsNullOrEmpty(stringValues))
                    {
                        if (stringValues.Count == 1)
                        {
                            var value = (string)stringValues;
                            if (!request.Headers.TryAddWithoutValidation(entry.OutboundHeaderName, value) && hasContent)
                            {
                                request.Content.Headers.TryAddWithoutValidation(entry.OutboundHeaderName, value);
                            }
                        }
                        else
                        {
                            var values = (string[])stringValues;
                            if (!request.Headers.TryAddWithoutValidation(entry.OutboundHeaderName, values) && hasContent)
                            {
                                request.Content.Headers.TryAddWithoutValidation(entry.OutboundHeaderName, values);
                            }
                        }
                    }
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
