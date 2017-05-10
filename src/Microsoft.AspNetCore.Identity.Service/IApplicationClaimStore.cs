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
        Task<IList<Claim>> GetClaimsAsync(TApplication user, CancellationToken cancellationToken);
        Task AddClaimsAsync(TApplication user, IEnumerable<Claim> claims, CancellationToken cancellationToken);
        Task ReplaceClaimAsync(TApplication user, Claim claim, Claim newClaim, CancellationToken cancellationToken);
        Task RemoveClaimsAsync(TApplication user, IEnumerable<Claim> claims, CancellationToken cancellationToken);
    }
}