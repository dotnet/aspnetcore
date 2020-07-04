// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Options for remote authentication.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationProviderOptions">The type of the underlying provider options.</typeparam>
    public class RemoteAuthenticationOptions<TRemoteAuthenticationProviderOptions> where TRemoteAuthenticationProviderOptions : new()
    {
        /// <summary>
        /// Gets or sets the provider options.
        /// </summary>
        public TRemoteAuthenticationProviderOptions ProviderOptions { get; } = new TRemoteAuthenticationProviderOptions();

        /// <summary>
        /// Gets or sets the <see cref="RemoteAuthenticationApplicationPathsOptions"/>.
        /// </summary>
        public RemoteAuthenticationApplicationPathsOptions AuthenticationPaths { get; } = new RemoteAuthenticationApplicationPathsOptions();

        /// <summary>
        /// Gets or sets the <see cref="RemoteAuthenticationUserOptions"/>.
        /// </summary>
        public RemoteAuthenticationUserOptions UserOptions { get; } = new RemoteAuthenticationUserOptions();
    }
}
