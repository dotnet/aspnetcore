// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Identity.Logging;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that manages SignIn operations for a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class SignInManager<TUser> where TUser : class
    {
        public SignInManager(UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor = null,
            ILoggerFactory logger = null)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }

            if (contextAccessor == null || contextAccessor.HttpContext == null)
            {
                throw new ArgumentNullException(nameof(contextAccessor));
            }
            if (claimsFactory == null)
            {
                throw new ArgumentNullException(nameof(claimsFactory));
            }

            UserManager = userManager;
            Context = contextAccessor.HttpContext;
            ClaimsFactory = claimsFactory;
            Options = optionsAccessor?.Options ?? new IdentityOptions();

            Logger = logger?.CreateLogger<SignInManager<TUser>>() ?? new Logger<SignInManager<TUser>>(new LoggerFactory());
        }

        protected internal virtual ILogger Logger { get; set; }
        internal UserManager<TUser> UserManager { get; private set; }
        internal HttpContext Context { get; private set; }
        internal IUserClaimsPrincipalFactory<TUser> ClaimsFactory { get; private set; }
        internal IdentityOptions Options { get; private set; }
       

        // Should this be a func?
        public virtual async Task<ClaimsPrincipal> CreateUserPrincipalAsync(TUser user) => await ClaimsFactory.CreateAsync(user);

        public virtual async Task<bool> CanSignInAsync(TUser user)
        {
            if (Options.SignIn.RequireConfirmedEmail && !(await UserManager.IsEmailConfirmedAsync(user)))
            {
                return Logger.Log(false);
            }
            if (Options.SignIn.RequireConfirmedPhoneNumber && !(await UserManager.IsPhoneNumberConfirmedAsync(user)))
            {
                return Logger.Log(false);
            }

            return Logger.Log(true);
        }

        public virtual async Task SignInAsync(TUser user, bool isPersistent, string authenticationMethod = null)
        {
            var userPrincipal = await CreateUserPrincipalAsync(user);
            // Review: should we guard against CreateUserPrincipal returning null?
            if (authenticationMethod != null)
            {
                userPrincipal.Identities.First().AddClaim(new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod));
            }
            Context.Response.SignIn(IdentityOptions.ApplicationCookieAuthenticationScheme,
                userPrincipal,
                new AuthenticationProperties() { IsPersistent = isPersistent });
        }

        public virtual void SignOut()
        {
            Context.Response.SignOut(IdentityOptions.ApplicationCookieAuthenticationScheme);
            Context.Response.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme);
            Context.Response.SignOut(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme);
        }

        private async Task<bool> IsLockedOut(TUser user)
        {
            return UserManager.SupportsUserLockout && await UserManager.IsLockedOutAsync(user);
        }

        private async Task<SignInResult> PreSignInCheck(TUser user)
        {
            if (!await CanSignInAsync(user))
            {
                return SignInResult.NotAllowed;
            }
            if (await IsLockedOut(user))
            {
                return SignInResult.LockedOut;
            }
            return null;
        }

        private Task ResetLockout(TUser user)
        {
            if (UserManager.SupportsUserLockout)
            {
                return UserManager.ResetAccessFailedCountAsync(user);
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// Validates that the claims identity has a security stamp matching the users
        /// Returns the user if it matches, null otherwise
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<TUser> ValidateSecurityStampAsync(ClaimsPrincipal principal, string userId)
        {
            var user = await UserManager.FindByIdAsync(userId);
            if (user != null && UserManager.SupportsUserSecurityStamp)
            {
                var securityStamp =
                    principal.FindFirstValue(Options.ClaimsIdentity.SecurityStampClaimType);
                if (securityStamp == await UserManager.GetSecurityStampAsync(user))
                {
                    return user;
                }
            }
            return null;
        }

        public virtual async Task<SignInResult> PasswordSignInAsync(TUser user, string password,
            bool isPersistent, bool shouldLockout)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using (await BeginLoggingScopeAsync(user))
            {
                var error = await PreSignInCheck(user);
                if (error != null)
                {
                    return Logger.Log(error);
                }
                if (await IsLockedOut(user))
                {
                    return Logger.Log(SignInResult.LockedOut);
                }
                if (await UserManager.CheckPasswordAsync(user, password))
                {
                    await ResetLockout(user);
                    return Logger.Log(await SignInOrTwoFactorAsync(user, isPersistent));
                }
                if (UserManager.SupportsUserLockout && shouldLockout)
                {
                    // If lockout is requested, increment access failed count which might lock out the user
                    await UserManager.AccessFailedAsync(user);
                    if (await UserManager.IsLockedOutAsync(user))
                    {

                        return Logger.Log(SignInResult.LockedOut);
                    }
                }
                return Logger.Log(SignInResult.Failed);
            }
        }

        public virtual async Task<SignInResult> PasswordSignInAsync(string userName, string password,
            bool isPersistent, bool shouldLockout)
        {
            var user = await UserManager.FindByNameAsync(userName);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            return await PasswordSignInAsync(user, password, isPersistent, shouldLockout);
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

        public virtual async Task<bool> IsTwoFactorClientRememberedAsync(TUser user)
        {
            var userId = await UserManager.GetUserIdAsync(user);
            var result = await Context.AuthenticateAsync(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme);
            return (result?.Principal != null && result.Principal.FindFirstValue(ClaimTypes.Name) == userId);
        }

        public virtual async Task RememberTwoFactorClientAsync(TUser user)
        {
            var userId = await UserManager.GetUserIdAsync(user);
            var rememberBrowserIdentity = new ClaimsIdentity(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            rememberBrowserIdentity.AddClaim(new Claim(ClaimTypes.Name, userId));
            Context.Response.SignIn(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme,
                new ClaimsPrincipal(rememberBrowserIdentity),
                new AuthenticationProperties { IsPersistent = true });
        }

        public virtual Task ForgetTwoFactorClientAsync()
        {
            Context.Response.SignOut(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme);
            return Task.FromResult(0);
        }

        public virtual async Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent,
            bool rememberClient)
        {
            var twoFactorInfo = await RetrieveTwoFactorInfoAsync();
            if (twoFactorInfo == null || twoFactorInfo.UserId == null)
            {
                return SignInResult.Failed;
            }
            var user = await UserManager.FindByIdAsync(twoFactorInfo.UserId);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            using (await BeginLoggingScopeAsync(user))
            {
                var error = await PreSignInCheck(user);
                if (error != null)
                {
                    return Logger.Log(error);
                }
                if (await UserManager.VerifyTwoFactorTokenAsync(user, provider, code))
                {
                    // When token is verified correctly, clear the access failed count used for lockout
                    await ResetLockout(user);
                    // Cleanup external cookie
                    if (twoFactorInfo.LoginProvider != null)
                    {
                        Context.Response.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme);
                    }
                    await SignInAsync(user, isPersistent, twoFactorInfo.LoginProvider);
                    if (rememberClient)
                    {
                        await RememberTwoFactorClientAsync(user);
                    }
                    await UserManager.ResetAccessFailedCountAsync(user);
                    await SignInAsync(user, isPersistent);
                    return Logger.Log(SignInResult.Success);
                }
                // If the token is incorrect, record the failure which also may cause the user to be locked out
                await UserManager.AccessFailedAsync(user);
                return Logger.Log(SignInResult.Failed);
            }
        }

        /// <summary>
        /// Returns the user who has started the two factor authentication process
        /// </summary>
        /// <returns></returns>
        public virtual async Task<TUser> GetTwoFactorAuthenticationUserAsync()
        {
            var info = await RetrieveTwoFactorInfoAsync();
            if (info == null)
            {
                return null;
            }

            return await UserManager.FindByIdAsync(info.UserId);
        }

        public virtual async Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent)
        {
            var user = await UserManager.FindByLoginAsync(loginProvider, providerKey);
            if (user == null)
            {
                return SignInResult.Failed;
            }

            using (await BeginLoggingScopeAsync(user))
            {
                var error = await PreSignInCheck(user);
                if (error != null)
                {
                    return Logger.Log(error);
                }
                return Logger.Log(await SignInOrTwoFactorAsync(user, isPersistent, loginProvider));
            }
        }

        private const string LoginProviderKey = "LoginProvider";
        private const string XsrfKey = "XsrfId";

        public virtual IEnumerable<AuthenticationDescription> GetExternalAuthenticationSchemes()
        {
            return Context.GetAuthenticationSchemes().Where(d => !string.IsNullOrEmpty(d.Caption));
        }

        public virtual async Task<ExternalLoginInfo> GetExternalLoginInfoAsync(string expectedXsrf = null)
        {
            var auth = await Context.AuthenticateAsync(IdentityOptions.ExternalCookieAuthenticationScheme);
            if (auth == null || auth.Principal == null || auth.Properties.Dictionary == null || !auth.Properties.Dictionary.ContainsKey(LoginProviderKey))
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

            var providerKey = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider = auth.Properties.Dictionary[LoginProviderKey] as string;
            if (providerKey == null || provider == null)
            {
                return null;
            }
            return new ExternalLoginInfo(auth.Principal, provider, providerKey, auth.Description.Caption);
        }

        public virtual AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string userId = null)
        {
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Dictionary[LoginProviderKey] = provider;
            if (userId != null)
            {
                properties.Dictionary[XsrfKey] = userId;
            }
            return properties;
        }

        private async Task<SignInResult> SignInOrTwoFactorAsync(TUser user, bool isPersistent, string loginProvider = null)
        {
            if (UserManager.SupportsUserTwoFactor &&
                await UserManager.GetTwoFactorEnabledAsync(user) &&
                (await UserManager.GetValidTwoFactorProvidersAsync(user)).Count > 0)
            {
                if (!await IsTwoFactorClientRememberedAsync(user))
                {
                    // Store the userId for use after two factor check
                    var userId = await UserManager.GetUserIdAsync(user);
                    Context.Response.SignIn(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme, StoreTwoFactorInfo(userId, loginProvider));
                    return SignInResult.TwoFactorRequired;
                }
            }
            // Cleanup external cookie
            if (loginProvider != null)
            {
                Context.Response.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme);
            }
            await SignInAsync(user, isPersistent, loginProvider);
            return SignInResult.Success;
        }

        private async Task<TwoFactorAuthenticationInfo> RetrieveTwoFactorInfoAsync()
        {
            var result = await Context.AuthenticateAsync(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme);
            if (result?.Principal != null)
            {
                return new TwoFactorAuthenticationInfo
                {
                    UserId = result.Principal.FindFirstValue(ClaimTypes.Name),
                    LoginProvider = result.Principal.FindFirstValue(ClaimTypes.AuthenticationMethod)
                };
            }
            return null;
        }

        protected virtual async Task<IDisposable> BeginLoggingScopeAsync(TUser user, [CallerMemberName] string methodName = null)
        {
            var state = Resources.FormatLoggingResultMessageForUser(methodName, await UserManager.GetUserIdAsync(user));
            return Logger.BeginScope(state);
        }
            

        internal static ClaimsPrincipal StoreTwoFactorInfo(string userId, string loginProvider)
        {
            var identity = new ClaimsIdentity(IdentityOptions.TwoFactorUserIdCookieAuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, userId));
            if (loginProvider != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
            }
            return new ClaimsPrincipal(identity);
        }
        internal class TwoFactorAuthenticationInfo
        {
            public string UserId { get; set; }
            public string LoginProvider { get; set; }
        }
    }
}