// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that manages SignIn operations for a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class SignInManager<TUser> where TUser : class
    {
        public SignInManager(UserManager<TUser> userManager, IContextAccessor<HttpContext> contextAccessor, 
            IClaimsIdentityFactory<TUser> claimsFactory, IOptionsAccessor<IdentityOptions> optionsAccessor)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }
            if (contextAccessor == null || contextAccessor.Value == null)
            {
                throw new ArgumentNullException(nameof(contextAccessor));
            }
            if (claimsFactory == null)
            {
                throw new ArgumentNullException(nameof(claimsFactory));
            }
            if (optionsAccessor == null || optionsAccessor.Options == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }
            UserManager = userManager;
            Context = contextAccessor.Value;
            ClaimsFactory = claimsFactory;
            Options = optionsAccessor.Options;
        }

        public UserManager<TUser> UserManager { get; private set; }
        public HttpContext Context { get; private set; }
        public IClaimsIdentityFactory<TUser> ClaimsFactory { get; private set; }
        public IdentityOptions Options { get; private set; }

        // Should this be a func?
        public virtual async Task<ClaimsIdentity> CreateUserIdentityAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await ClaimsFactory.CreateAsync(user);
        }

        //public virtual async Task<bool> CanSignInAsync(TUser user,
        //    CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    if (Options.SignIn.RequireConfirmedEmail && !(await UserManager.IsEmailConfirmedAsync(user, cancellationToken)))
        //    {
        //        return false;
        //    }
        //    if (Options.SignIn.RequireConfirmedPhoneNumber && !(await UserManager.IsPhoneNumberConfirmedAsync(user, cancellationToken)))
        //    {
        //        return false;
        //    }
        //    return true;
        //}

        public virtual async Task SignInAsync(TUser user, bool isPersistent, string authenticationMethod = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var userIdentity = await CreateUserIdentityAsync(user);
            if (authenticationMethod != null)
            {
                userIdentity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod));
            }
            Context.Response.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, userIdentity);
        }

        // TODO: Should this be async?
        public virtual void SignOut()
        {
            Context.Response.SignOut(IdentityOptions.ApplicationCookieAuthenticationType);
        }

        private async Task<bool> IsLockedOut(TUser user, CancellationToken token)
        {
            return UserManager.SupportsUserLockout && await UserManager.IsLockedOutAsync(user, token);
        }

        /// <summary>
        /// Validates that the claims identity has a security stamp matching the users
        /// Returns the user if it matches, null otherwise
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<TUser> ValidateSecurityStampAsync(ClaimsIdentity identity, string userId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var user = await UserManager.FindByIdAsync(userId, cancellationToken);
            if (user != null && UserManager.SupportsUserSecurityStamp)
            {
                var securityStamp =
                    identity.FindFirstValue(Options.ClaimsIdentity.SecurityStampClaimType);
                if (securityStamp == await UserManager.GetSecurityStampAsync(user, cancellationToken))
                {
                    return user;
                }
            }
            return null;
        }

        public virtual async Task<SignInStatus> PasswordSignInAsync(string userName, string password, 
            bool isPersistent, bool shouldLockout, CancellationToken cancellationToken = default(CancellationToken))
        {
            var user = await UserManager.FindByNameAsync(userName, cancellationToken);
            if (user == null)
            {
                return SignInStatus.Failure;
            }
            if (await IsLockedOut(user, cancellationToken))
            {
                return SignInStatus.LockedOut;
            }
            if (await UserManager.CheckPasswordAsync(user, password, cancellationToken))
            {
                return await SignInOrTwoFactorAsync(user, isPersistent, cancellationToken);
            }
            if (UserManager.SupportsUserLockout && shouldLockout)
            {
                // If lockout is requested, increment access failed count which might lock out the user
                await UserManager.AccessFailedAsync(user, cancellationToken);
                if (await UserManager.IsLockedOutAsync(user, cancellationToken))
                {
                    return SignInStatus.LockedOut;
                }
            }
            return SignInStatus.Failure;
        }

        private static ClaimsIdentity CreateIdentity(TwoFactorAuthenticationInfo info)
        {
            if (info == null)
            {
                return null;
            }
            var identity = new ClaimsIdentity(IdentityOptions.TwoFactorUserIdCookieAuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, info.UserId));
            if (info.LoginProvider != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, info.LoginProvider));
            }
            return identity;
        }

        public virtual async Task<bool> SendTwoFactorCodeAsync(string provider,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var twoFactorInfo = await RetrieveTwoFactorInfoAsync(cancellationToken);
            if (twoFactorInfo == null || twoFactorInfo.UserId == null)
            {
                return false;
            }

            var user = await UserManager.FindByIdAsync(twoFactorInfo.UserId, cancellationToken);
            if (user == null)
            {
                return false;
            }
            var token = await UserManager.GenerateTwoFactorTokenAsync(user, provider, cancellationToken);
            // See IdentityConfig.cs to plug in Email/SMS services to actually send the code
            await UserManager.NotifyTwoFactorTokenAsync(user, provider, token, cancellationToken);
            return true;
        }

        public async Task<bool> IsTwoFactorClientRememberedAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var userId = await UserManager.GetUserIdAsync(user, cancellationToken);
            var result =
                await Context.AuthenticateAsync(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            return (result != null && result.Identity != null && result.Identity.Name == userId);
        }

        public virtual async Task RememberTwoFactorClientAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var userId = await UserManager.GetUserIdAsync(user, cancellationToken);
            var rememberBrowserIdentity = new ClaimsIdentity(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            rememberBrowserIdentity.AddClaim(new Claim(ClaimTypes.Name, userId));
            Context.Response.SignIn(new AuthenticationProperties { IsPersistent = true }, rememberBrowserIdentity);
        }

        public virtual Task ForgetTwoFactorClientAsync()
        {
            Context.Response.SignOut(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            return Task.FromResult(0);
        }

        public virtual async Task<SignInStatus> TwoFactorSignInAsync(string provider, string code, bool isPersistent, 
            bool rememberClient, CancellationToken cancellationToken = default(CancellationToken))
        {
            var twoFactorInfo = await RetrieveTwoFactorInfoAsync(cancellationToken);
            if (twoFactorInfo == null || twoFactorInfo.UserId == null)
            {
                return SignInStatus.Failure;
            }
            var user = await UserManager.FindByIdAsync(twoFactorInfo.UserId, cancellationToken);
            if (user == null)
            {
                return SignInStatus.Failure;
            }
            if (await IsLockedOut(user, cancellationToken))
            {
                return SignInStatus.LockedOut;
            }
            if (await UserManager.VerifyTwoFactorTokenAsync(user, provider, code, cancellationToken))
            {
                // When token is verified correctly, clear the access failed count used for lockout
                await UserManager.ResetAccessFailedCountAsync(user, cancellationToken);
                // Cleanup external cookie
                if (twoFactorInfo.LoginProvider != null)
                {
                    Context.Response.SignOut(IdentityOptions.ExternalCookieAuthenticationType);
                }
                await SignInAsync(user, isPersistent, twoFactorInfo.LoginProvider, cancellationToken);
                if (rememberClient)
                {
                    await RememberTwoFactorClientAsync(user, cancellationToken);
                }
                return SignInStatus.Success;
            }
            // If the token is incorrect, record the failure which also may cause the user to be locked out
            await UserManager.AccessFailedAsync(user, cancellationToken);
            return SignInStatus.Failure;
        }

        /// <summary>
        /// Returns the user who has started the two factor authentication process
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<TUser> GetTwoFactorAuthenticationUserAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var info = await RetrieveTwoFactorInfoAsync(cancellationToken);
            if (info == null)
            {
                return null;
            }

            return await UserManager.FindByIdAsync(info.UserId, cancellationToken);
        }

        public virtual async Task<SignInStatus> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var user = await UserManager.FindByLoginAsync(loginProvider, providerKey, cancellationToken);
            if (user == null)
            {
                return SignInStatus.Failure;
            }
            if (await IsLockedOut(user, cancellationToken))
            {
                return SignInStatus.LockedOut;
            }
            return await SignInOrTwoFactorAsync(user, isPersistent, cancellationToken, loginProvider);
        }

        private const string LoginProviderKey = "LoginProvider";
        private const string XsrfKey = "XsrfId";

        public virtual IEnumerable<AuthenticationDescription> GetExternalAuthenticationTypes()
        {
            return Context.GetAuthenticationTypes().Where(d => !string.IsNullOrEmpty(d.Caption));
        }

        public virtual async Task<ExternalLoginInfo> GetExternalLoginInfoAsync(string expectedXsrf = null, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var auth = await Context.AuthenticateAsync(IdentityOptions.ExternalCookieAuthenticationType);
            if (auth == null || auth.Identity == null || auth.Properties.Dictionary == null || !auth.Properties.Dictionary.ContainsKey(LoginProviderKey))
            {
                return null;
            }

            if (expectedXsrf != null)
            {
                if (!auth.Properties.Dictionary.ContainsKey(XsrfKey))
                {
                    return null;
                }
                var userId = auth.Properties.Dictionary[XsrfKey] as string;
                if (userId != expectedXsrf)
                {
                    return null;
                }
            }

            var providerKey = auth.Identity.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider = auth.Properties.Dictionary[LoginProviderKey] as string;
            if (providerKey == null || provider == null)
            {
                return null;
            }
            return new ExternalLoginInfo(auth.Identity, provider, providerKey, auth.Description.Caption);
        }

        public AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string userId = null)
        {
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Dictionary[LoginProviderKey] = provider;
            if (userId != null)
            {
                properties.Dictionary[XsrfKey] = userId;
            }
            return properties;
        }

        private async Task<SignInStatus> SignInOrTwoFactorAsync(TUser user, bool isPersistent,
            CancellationToken cancellationToken, string loginProvider = null)
        {
            if (UserManager.SupportsUserTwoFactor && 
                await UserManager.GetTwoFactorEnabledAsync(user, cancellationToken) &&
                (await UserManager.GetValidTwoFactorProvidersAsync(user, cancellationToken)).Count > 0)
            {
                if (!await IsTwoFactorClientRememberedAsync(user, cancellationToken))
                {
                    // Store the userId for use after two factor check
                    var userId = await UserManager.GetUserIdAsync(user, cancellationToken);
                    Context.Response.SignIn(StoreTwoFactorInfo(userId, loginProvider));
                    return SignInStatus.RequiresVerification;
                }
            }
            // Cleanup external cookie
            if (loginProvider != null)
            {
                Context.Response.SignOut(IdentityOptions.ExternalCookieAuthenticationType);
            }
            await SignInAsync(user, isPersistent, loginProvider, cancellationToken);
            return SignInStatus.Success;
        }

        private async Task<TwoFactorAuthenticationInfo> RetrieveTwoFactorInfoAsync(CancellationToken cancellationToken)
        {
            var result = await Context.AuthenticateAsync(IdentityOptions.TwoFactorUserIdCookieAuthenticationType);
            if (result != null && result.Identity != null)
            {
                return new TwoFactorAuthenticationInfo
                {
                    UserId = result.Identity.Name,
                    LoginProvider = result.Identity.FindFirstValue(ClaimTypes.AuthenticationMethod)
                };
            }
            return null;
        }

        internal static ClaimsIdentity StoreTwoFactorInfo(string userId, string loginProvider)
        {
            var identity = new ClaimsIdentity(IdentityOptions.TwoFactorUserIdCookieAuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, userId));
            if (loginProvider != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
            }
            return identity;
        }

        internal class TwoFactorAuthenticationInfo
        {
            public string UserId { get; set; }
            public string LoginProvider { get; set; }
        }
    }
}