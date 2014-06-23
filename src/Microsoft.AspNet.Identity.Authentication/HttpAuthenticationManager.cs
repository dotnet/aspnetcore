// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity.Authentication
{
    public class HttpAuthenticationManager : IAuthenticationManager
    {
        public static readonly string TwoFactorUserIdAuthenticationType = "Microsoft.AspNet.Identity.TwoFactor.UserId";
        public static readonly string TwoFactorRememberedAuthenticationType = "Microsoft.AspNet.Identity.TwoFactor.Remembered";

        public HttpAuthenticationManager(IContextAccessor<HttpContext> contextAccessor)
        {
            Context = contextAccessor.Value;
        }

        public HttpContext Context { get; private set; }

        public void ForgetClient()
        {
            Context.Response.SignOut(TwoFactorRememberedAuthenticationType);
        }

        public async Task<bool> IsClientRememeberedAsync(string userId)
        {
            var result =
                await Context.AuthenticateAsync(TwoFactorRememberedAuthenticationType);
            return (result != null && result.Identity != null && result.Identity.Name == userId);
        }

        public void RememberClient(string userId)
        {
            var rememberBrowserIdentity = new ClaimsIdentity(TwoFactorRememberedAuthenticationType);
            rememberBrowserIdentity.AddClaim(new Claim(ClaimTypes.Name, userId));
            Context.Response.SignIn(rememberBrowserIdentity);
        }

        public async Task<string> RetrieveUserId()
        {
            var result = await Context.AuthenticateAsync(TwoFactorUserIdAuthenticationType);
            if (result != null && result.Identity != null)
            {
                return result.Identity.Name;
            }
            return null;
        }

        public void SignIn(ClaimsIdentity identity, bool isPersistent)
        {
            Context.Response.SignIn(identity, new AuthenticationProperties { IsPersistent = isPersistent });
        }
        public void SignOut(string authenticationType)
        {
            Context.Response.SignOut(authenticationType);
        }

        public Task StoreUserId(string userId)
        {
            var userIdentity = new ClaimsIdentity(TwoFactorUserIdAuthenticationType);
            userIdentity.AddClaim(new Claim(ClaimTypes.Name, userId));
            Context.Response.SignIn(userIdentity);
            return Task.FromResult(0);
        }
    }
}