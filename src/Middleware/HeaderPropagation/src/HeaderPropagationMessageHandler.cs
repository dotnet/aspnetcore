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

        public HeaderPropagationMessageHandler(IOptions<HeaderPropagationOptions> options, HeaderPropagationValues values)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;

            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            foreach ((var headerName, var entry) in _options.Headers)
            {
                var outputName = !string.IsNullOrEmpty(entry?.OutboundHeaderName) ? entry.OutboundHeaderName : headerName;

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
