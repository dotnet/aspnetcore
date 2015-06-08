// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
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
    /// Provides the APIs for user sign in.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class SignInManager<TUser> where TUser : class
    {
        private const string LoginProviderKey = "LoginProvider";
        private const string XsrfKey = "XsrfId";

        /// <summary>
        /// Creates a new instance of <see cref="SignInManager{TUser}"/>.
        /// </summary>
        /// <param name="userManager">An instance of <see cref="UserManager"/> used to retrieve users from and persist users.</param>
        /// <param name="contextAccessor">The accessor used to access the <see cref="HttpContext"/>.</param>
        /// <param name="claimsFactory">The factory to use to create claims principals for a user.</param>
        /// <param name="optionsAccessor">The accessor used to access the <see cref="IdentityOptions"/>.</param>
        /// <param name="logger">The logger used to log messages, warnings and errors.</param>
        public SignInManager(UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<TUser>> logger)
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
            Logger = logger;
        }

        /// <summary>
        /// Gets the <see cref="ILogger"/> used to log messages from the manager.
        /// </summary>
        /// <value>
        /// The <see cref="ILogger"/> used to log messages from the manager.
        /// </value>
        protected internal virtual ILogger Logger { get; set; }
        internal UserManager<TUser> UserManager { get; set; }
        internal HttpContext Context { get; set; }
        internal IUserClaimsPrincipalFactory<TUser> ClaimsFactory { get; set; }
        internal IdentityOptions Options { get; set; }

        /// <summary>
        /// Creates a <see cref="ClaimsPrincipal"/> for the specified <paramref name="user"/>, as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user to create a <see cref="ClaimsPrincipal"/> for.</param>
        /// <returns>The task object representing the asynchronous operation, containing the ClaimsPrincipal for the specified user.</returns>
        public virtual async Task<ClaimsPrincipal> CreateUserPrincipalAsync(TUser user) => await ClaimsFactory.CreateAsync(user);

        /// <summary>
        /// Returns a flag indicating whether the specified user can sign in.
        /// </summary>
        /// <param name="user">The user whose sign-in status should be returned.</param>
        /// <returns>
        /// The task object representing the asynchronous operation, containing a flag that is true
        /// if the specified user can sign-in, otherwise false.
        /// </returns>
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

        /// <summary>
        /// Regenerates the user's application cookie, whilst preserving the existing
        /// AuthenticationProperties like rememberMe, as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose sign-in cookie should be refreshed.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task RefreshSignInAsync(TUser user)
        {
            var authResult = await Context.Authentication.AuthenticateAsync(IdentityOptions.ApplicationCookieAuthenticationScheme);
            var properties = authResult?.Properties ?? new AuthenticationProperties();
            var authenticationMethod = authResult?.Principal?.FindFirstValue(ClaimTypes.AuthenticationMethod);
            await SignInAsync(user, properties, authenticationMethod);
        }

        /// <summary>
        /// Signs in the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to sign-in.</param>
        /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
        /// <param name="authenticationMethod">Name of the method used to authenticate the user.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual Task SignInAsync(TUser user, bool isPersistent, string authenticationMethod = null)
        {
            return SignInAsync(user, new AuthenticationProperties { IsPersistent = isPersistent }, authenticationMethod);
        }

        /// <summary>
        /// Signs in the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to sign-in.</param>
        /// <param name="authenticationProperties">Properties applied to the login and authentication cookie.</param>
        /// <param name="authenticationMethod">Name of the method used to authenticate the user.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task SignInAsync(TUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null)
        {
            var userPrincipal = await CreateUserPrincipalAsync(user);
            // Review: should we guard against CreateUserPrincipal returning null?
            if (authenticationMethod != null)
            {
                userPrincipal.Identities.First().AddClaim(new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod));
            }
            Context.Authentication.SignIn(IdentityOptions.ApplicationCookieAuthenticationScheme,
                userPrincipal,
                authenticationProperties ?? new AuthenticationProperties());
        }

        /// <summary>
        /// Signs the current user out of the application.
        /// </summary>
        public virtual void SignOut()
        {
            Context.Authentication.SignOut(IdentityOptions.ApplicationCookieAuthenticationScheme);
            Context.Authentication.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme);
            Context.Authentication.SignOut(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme);
        }

        /// <summary>
        /// Validates the security stamp for the specified <paramref name="principal"/> against
        /// the persisted stamp for the <paramref name="userId"/>, as an asynchronous operation.
        /// </summary>
        /// <param name="principal">The principal whose stamp should be validated.</param>
        /// <param name="userId">The ID for the user.</param>
        /// <returns>The task object representing the asynchronous operation. The task will contain the <typeparamref name="TUser"/>
        /// if the stamp matches the persisted value, otherwise it will return false.</returns>
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

        /// <summary>
        /// Attempts to sign in the specified <paramref name="user"/> and <paramref name="password"/> combination
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user to sign in.</param>
        /// <param name="password">The password to attempt to sign in with.</param>
        /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
        /// <param name="shouldLockout">Flag indicating if the user account should be locked if the sign in fails.</param>
        /// <returns>The task object representing the asynchronous operation containing the <see name="SignInResult"/>
        /// for the sign-in attempt.</returns>
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

        /// <summary>
        /// Attempts to sign in the specified <paramref name="userName"/> and <paramref name="password"/> combination
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="userName">The user name to sign in.</param>
        /// <param name="password">The password to attempt to sign in with.</param>
        /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
        /// <param name="shouldLockout">Flag indicating if the user account should be locked if the sign in fails.</param>
        /// <returns>The task object representing the asynchronous operation containing the <see name="SignInResult"/>
        /// for the sign-in attempt.</returns>
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

        /// <summary>
        /// Returns a flag indicating if the current client browser has been remembered by two factor authentication
        /// for the user attempting to login, as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user attempting to login.</param>
        /// <returns>
        /// The task object representing the asynchronous operation containing true if the browser has been remembered
        /// for the current user.
        /// </returns>
        public virtual async Task<bool> IsTwoFactorClientRememberedAsync(TUser user)
        {
            var userId = await UserManager.GetUserIdAsync(user);
            var result = await Context.Authentication.AuthenticateAsync(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme);
            return (result?.Principal != null && result.Principal.FindFirstValue(ClaimTypes.Name) == userId);
        }

        /// <summary>
        /// Sets a flag on the browser to indicate the user has selected "Remember this browser" for two factor authentication purposes,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user who choose "remember this browser".</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task RememberTwoFactorClientAsync(TUser user)
        {
            var userId = await UserManager.GetUserIdAsync(user);
            var rememberBrowserIdentity = new ClaimsIdentity(IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            rememberBrowserIdentity.AddClaim(new Claim(ClaimTypes.Name, userId));
            Context.Authentication.SignIn(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme,
                new ClaimsPrincipal(rememberBrowserIdentity),
                new AuthenticationProperties { IsPersistent = true });
        }

        /// <summary>
        /// Clears the "Remember this browser flag" from the current browser, as an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual Task ForgetTwoFactorClientAsync()
        {
            Context.Authentication.SignOut(IdentityOptions.TwoFactorRememberMeCookieAuthenticationScheme);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Validates the two faction sign in code and creates and signs in the user, as an asynchronous operation.
        /// </summary>
        /// <param name="provider">The two factor authentication provider to validate the code against.</param>
        /// <param name="code">The two factor authentication code to validate.</param>
        /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
        /// <param name="rememberClient">Flag indicating whether the current browser should be remember, suppressing all further 
        /// two factor authentication prompts.</param>
        /// <returns>The task object representing the asynchronous operation containing the <see name="SignInResult"/>
        /// for the sign-in attempt.</returns>
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
                        Context.Authentication.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme);
                    }
                    if (rememberClient)
                    {
                        await RememberTwoFactorClientAsync(user);
                    }
                    await UserManager.ResetAccessFailedCountAsync(user);
                    await SignInAsync(user, isPersistent, twoFactorInfo.LoginProvider);
                    return Logger.Log(SignInResult.Success);
                }
                // If the token is incorrect, record the failure which also may cause the user to be locked out
                await UserManager.AccessFailedAsync(user);
                return Logger.Log(SignInResult.Failed);
            }
        }

        /// <summary>
        /// Gets the <typeparamref name="TUser"/> for the current two factor authentication login, as an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation containing the <typeparamref name="TUser"/>
        /// for the sign-in attempt.</returns>
        public virtual async Task<TUser> GetTwoFactorAuthenticationUserAsync()
        {
            var info = await RetrieveTwoFactorInfoAsync();
            if (info == null)
            {
                return null;
            }

            return await UserManager.FindByIdAsync(info.UserId);
        }

        /// <summary>
        /// Signs in a user via a previously registered third party login, as an asynchronous operation.
        /// </summary>
        /// <param name="loginProvider">The login provider to use.</param>
        /// <param name="providerKey">The unique provider identifier for the user.</param>
        /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
        /// <returns>The task object representing the asynchronous operation containing the <see name="SignInResult"/>
        /// for the sign-in attempt.</returns>
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

        /// <summary>
        /// Gets a collection of <see cref="AuthenticationDescription"/>s for the known external login providers.
        /// </summary>
        /// <returns>A collection of <see cref="AuthenticationDescription"/>s for the known external login providers.</returns>
        public virtual IEnumerable<AuthenticationDescription> GetExternalAuthenticationSchemes()
        {
            return Context.Authentication.GetAuthenticationSchemes().Where(d => !string.IsNullOrEmpty(d.Caption));
        }

        /// <summary>
        /// Gets the external login information for the current login, as an asynchronous operation.
        /// </summary>
        /// <param name="expectedXsrf">Flag indication whether a Cross Site Request Forgery token was expected in the current request.</param>
        /// <returns>The task object representing the asynchronous operation containing the <see name="ExternalLoginInfo"/>
        /// for the sign-in attempt.</returns>
        public virtual async Task<ExternalLoginInfo> GetExternalLoginInfoAsync(string expectedXsrf = null)
        {
            var auth = await Context.Authentication.AuthenticateAsync(IdentityOptions.ExternalCookieAuthenticationScheme);
            if (auth == null || auth.Principal == null || auth.Properties.Items == null || !auth.Properties.Items.ContainsKey(LoginProviderKey))
            {
                return null;
            }

            if (expectedXsrf != null)
            {
                if (!auth.Properties.Items.ContainsKey(XsrfKey))
                {
                    return null;
                }
                var userId = auth.Properties.Items[XsrfKey] as string;
                if (userId != expectedXsrf)
                {
                    return null;
                }
            }

            var providerKey = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var provider = auth.Properties.Items[LoginProviderKey] as string;
            if (providerKey == null || provider == null)
            {
                return null;
            }
            return new ExternalLoginInfo(auth.Principal, provider, providerKey, auth.Description.Caption);
        }
        
        /// <summary>
        /// Configures the redirect URL and user identifier for the specified external login <paramref name="provider"/>.
        /// </summary>
        /// <param name="provider">The provider to configure.</param>
        /// <param name="redirectUrl">The external login URL users should be redirected to during the login glow.</param>
        /// <param name="userId">The current user's identifier, which will be used to provide CSRF protection.</param>
        /// <returns>A configured <see cref="AuthenticationProperties"/>.</returns>
        public virtual AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string userId = null)
        {
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[LoginProviderKey] = provider;
            if (userId != null)
            {
                properties.Items[XsrfKey] = userId;
            }
            return properties;
        }

        /// <summary>
        /// Starts a scope for wrapping log messages, as an asynchronous operation.
        /// </summary>
        /// <param name="user">The current user.</param>
        /// <param name="methodName">The method that called this method.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected virtual async Task<IDisposable> BeginLoggingScopeAsync(TUser user, [CallerMemberName] string methodName = null)
        {
            var state = Resources.FormatLoggingResultMessageForUser(methodName, await UserManager.GetUserIdAsync(user));
            return Logger?.BeginScope(state);
        }

        /// <summary>
        /// Creates a claims principal for the specified 2fa information.
        /// </summary>
        /// <param name="userId">The user whose is logging in via 2fa.</param>
        /// <param name="loginProvider">The 2fa provider.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> containing the user 2fa information.</returns>
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
                    Context.Authentication.SignIn(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme, StoreTwoFactorInfo(userId, loginProvider));
                    return SignInResult.TwoFactorRequired;
                }
            }
            // Cleanup external cookie
            if (loginProvider != null)
            {
                Context.Authentication.SignOut(IdentityOptions.ExternalCookieAuthenticationScheme);
            }
            await SignInAsync(user, isPersistent, loginProvider);
            return SignInResult.Success;
        }

        private async Task<TwoFactorAuthenticationInfo> RetrieveTwoFactorInfoAsync()
        {
            var result = await Context.Authentication.AuthenticateAsync(IdentityOptions.TwoFactorUserIdCookieAuthenticationScheme);
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

        internal class TwoFactorAuthenticationInfo
        {
            public string UserId { get; set; }
            public string LoginProvider { get; set; }
        }
    }
}