// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface IApplicationClaimsPrincipalFactory<TApplication>
    {
        Task<ClaimsPrincipal> CreateAsync(TApplication application);
    }
}
