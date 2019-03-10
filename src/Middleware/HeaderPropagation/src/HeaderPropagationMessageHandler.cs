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
    public class HeaderPropagationMessageHandler : DelegatingHandler
    {
        private readonly HeaderPropagationState _state;
        private readonly HeaderPropagationOptions _options;

        public HeaderPropagationMessageHandler(IOptions<HeaderPropagationOptions> options, HeaderPropagationState state)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;

            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            foreach (var header in _options.Headers)
            {
                var outputName = !string.IsNullOrEmpty(header.OutputName) ? header.OutputName : header.InputName;

                if (_state.Headers.TryGetValue(header.InputName, out var values) &&
                    !StringValues.IsNullOrEmpty(values) &&
                    (header.AlwaysAdd || !request.Headers.Contains(outputName)))
                {
                    request.Headers.TryAddWithoutValidation(outputName, (string[]) values);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
