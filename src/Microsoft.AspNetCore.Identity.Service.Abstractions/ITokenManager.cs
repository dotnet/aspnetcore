// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface ITokenManager
    {
        Task IssueTokensAsync(TokenGeneratingContext context);
        Task<AuthorizationGrant> ExchangeTokenAsync(OpenIdConnectMessage message);
    }
}
