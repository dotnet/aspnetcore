// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IApplicationClaimStore<TApplication> : IApplicationStore<TApplication> where TApplication : class
    {
        Task<IList<Claim>> GetClaimsAsync(TApplication application, CancellationToken cancellationToken);
        Task AddClaimsAsync(TApplication application, IEnumerable<Claim> claims, CancellationToken cancellationToken);
        Task ReplaceClaimAsync(TApplication application, Claim claim, Claim newClaim, CancellationToken cancellationToken);
        Task RemoveClaimsAsync(TApplication application, IEnumerable<Claim> claims, CancellationToken cancellationToken);
    }
}