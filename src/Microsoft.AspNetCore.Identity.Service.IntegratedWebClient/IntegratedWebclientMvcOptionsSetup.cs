// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.IntegratedWebClient
{
    public class IntegratedWebclientMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly IOptions<IntegratedWebClientOptions> _webClientOptions;

        public IntegratedWebclientMvcOptionsSetup(IOptions<IntegratedWebClientOptions> webClientOptions)
        {
            _webClientOptions = webClientOptions;
        }

        public void Configure(MvcOptions options)
        {
            if (!string.IsNullOrEmpty(_webClientOptions.Value.TokenRedirectUrn))
            {
                options.Conventions.Add(new IntegratedWebClientModelConvention(_webClientOptions));
            }
        }
    }
}
