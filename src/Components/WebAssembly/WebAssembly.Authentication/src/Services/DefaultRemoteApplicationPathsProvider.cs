// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    internal class DefaultRemoteApplicationPathsProvider<TProviderOptions> : IRemoteAuthenticationPathsProvider where TProviderOptions : class, new()
    {
        private readonly IOptions<RemoteAuthenticationOptions<TProviderOptions>> _options;

        public DefaultRemoteApplicationPathsProvider(IOptionsSnapshot<RemoteAuthenticationOptions<TProviderOptions>> options)
        {
            _options = options;
        }

        public RemoteAuthenticationApplicationPathsOptions ApplicationPaths => _options.Value.AuthenticationPaths;
    }
}
