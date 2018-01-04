// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IApplicationScopeStore<TApplication> : IApplicationStore<TApplication>
        where TApplication : class
    {
        Task<IEnumerable<string>> FindScopesAsync(TApplication application, CancellationToken cancellationToken);
        Task<IdentityServiceResult> AddScopeAsync(TApplication application, string scope, CancellationToken cancellationToken);
        Task<IdentityServiceResult> UpdateScopeAsync(TApplication application, string oldScope, string newScope, CancellationToken cancellationToken);
        Task<IdentityServiceResult> RemoveScopeAsync(TApplication application, string scope, CancellationToken cancellationToken);
    }
}
