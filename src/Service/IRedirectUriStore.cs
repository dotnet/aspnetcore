// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IRedirectUriStore<TApplication> : IApplicationStore<TApplication>
        where TApplication : class
    {
        Task<IEnumerable<string>> FindRegisteredUrisAsync(TApplication app, CancellationToken cancellationToken);
        Task<IEnumerable<string>> FindRegisteredLogoutUrisAsync(TApplication app, CancellationToken cancellationToken);
        Task<IdentityServiceResult> RegisterRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken);
        Task<IdentityServiceResult> RegisterLogoutRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken);
        Task<IdentityServiceResult> UnregisterRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken);
        Task<IdentityServiceResult> UnregisterLogoutRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken);
        Task<IdentityServiceResult> UpdateRedirectUriAsync(TApplication app, string oldRedirectUri, string newRedirectUri, CancellationToken cancellationToken);
        Task<IdentityServiceResult> UpdateLogoutRedirectUriAsync(TApplication app, string oldRedirectUri, string newRedirectUri, CancellationToken cancellationToken);
    }
}
