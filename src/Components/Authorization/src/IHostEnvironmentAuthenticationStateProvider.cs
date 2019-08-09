// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// An interface implemented by <see cref="AuthenticationStateProvider"/> classes that can receive authentication
    /// state information from the host environment.
    /// </summary>
    public interface IHostEnvironmentAuthenticationStateProvider
    {
        /// <summary>
        /// Supplies updated authentication state data to the <see cref="AuthenticationStateProvider"/>.
        /// </summary>
        /// <param name="authenticationStateTask">A task that resolves with the updated <see cref="AuthenticationState"/>.</param>
        void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask);
    }
}
