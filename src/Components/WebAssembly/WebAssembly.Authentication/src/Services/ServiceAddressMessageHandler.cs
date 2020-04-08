// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class ServiceAddressMessageHandler : DelegatingHandler
    {
        private readonly Uri _baseAddress;

        public ServiceAddressMessageHandler(string baseAddress)
        {
            _baseAddress = new Uri(baseAddress, UriKind.Absolute);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_baseAddress.IsBaseOf(request.RequestUri))
            {
                throw new InvalidOperationException($"Request URI '{request.RequestUri}' is not for the configured service address '{_baseAddress}'");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
