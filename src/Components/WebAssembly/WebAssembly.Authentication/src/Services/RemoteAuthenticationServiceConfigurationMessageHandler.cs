// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class RemoteAuthenticationServiceConfigurationMessageHandler : DelegatingHandler
    {
        internal const string AccessTokenRequestOptionsKey = "Microsoft.AspNetCore.Components.WebAssembly.Authentication.AccessTokenRequestOptions";
        private readonly string _optionsKey;

        public RemoteAuthenticationServiceConfigurationMessageHandler(string optionsKey)
        {
            _optionsKey = optionsKey;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Properties[AccessTokenRequestOptionsKey] = _optionsKey;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
