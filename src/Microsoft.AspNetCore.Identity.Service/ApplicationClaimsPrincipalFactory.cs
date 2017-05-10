// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ApplicationClaimsPrincipalFactory<TApplication>
        : IApplicationClaimsPrincipalFactory<TApplication>
        where TApplication : class
    {
        public ApplicationClaimsPrincipalFactory(ApplicationManager<TApplication> applicationManager)
        {
            ApplicationManager = applicationManager;
        }

        public ApplicationManager<TApplication> ApplicationManager { get; }

        public async Task<ClaimsPrincipal> CreateAsync(TApplication application)
        {
            var appId = await ApplicationManager.GetApplicationIdAsync(application);
            var clientId = await ApplicationManager.GetApplicationClientIdAsync(application);

            var applicationIdentity = new ClaimsIdentity();
            applicationIdentity.AddClaim(new Claim(IdentityServiceClaimTypes.ObjectId, appId));
            applicationIdentity.AddClaim(new Claim(IdentityServiceClaimTypes.ClientId, clientId));

            var logoutRedirectUris = await ApplicationManager.FindRegisteredLogoutUrisAsync(application);
            foreach (var logoutRedirectUri in logoutRedirectUris)
            {
                applicationIdentity.AddClaim(new Claim(IdentityServiceClaimTypes.LogoutRedirectUri, logoutRedirectUri));
            }

            return new ClaimsPrincipal(applicationIdentity);
        }
    }
}
