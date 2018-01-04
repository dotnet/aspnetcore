// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IApplicationStore<TApplication> : IDisposable where TApplication : class
    {
        Task<IdentityServiceResult> CreateAsync(TApplication application, CancellationToken cancellationToken);
        Task<IdentityServiceResult> UpdateAsync(TApplication application, CancellationToken cancellationToken);
        Task<IdentityServiceResult> DeleteAsync(TApplication application, CancellationToken cancellationToken);
        Task<TApplication> FindByIdAsync(string applicationId, CancellationToken cancellationToken);
        Task<IEnumerable<TApplication>> FindByUserIdAsync(string applicationId, CancellationToken cancellationToken);
        Task<TApplication> FindByClientIdAsync(string clientId, CancellationToken cancellationToken);
        Task<TApplication> FindByNameAsync(string name, CancellationToken cancellationToken);
        Task<string> GetApplicationIdAsync(TApplication application, CancellationToken cancellationToken);
        Task<string> GetApplicationNameAsync(TApplication application, CancellationToken cancellationToken);
        Task SetApplicationNameAsync(TApplication application, string name, CancellationToken cancellationToken);
        Task<string> GetApplicationClientIdAsync(TApplication application, CancellationToken cancellationToken);
        Task<string> GetApplicationUserIdAsync(TApplication application, CancellationToken cancellationToken);

    }
}
