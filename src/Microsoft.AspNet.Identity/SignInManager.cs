// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that manages SignIn operations for a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class SignInManager<TUser> where TUser : class
    {
        public SignInManager(UserManager<TUser> userManager, IAuthenticationManager authenticationManager, 
            IClaimsIdentityFactory<TUser> claimsFactory, IOptionsAccessor<IdentityOptions> optionsAccessor)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException("userManager");
            }
            if (authenticationManager == null)
            {
                throw new ArgumentNullException("authenticationManager");
            }
            if (claimsFactory == null)
            {
                throw new ArgumentNullException("claimsFactory");
            }
            if (optionsAccessor == null || optionsAccessor.Options == null)
            {
                throw new ArgumentNullException("optionsAccessor");
            }
            UserManager = userManager;
            AuthenticationManager = authenticationManager;
            ClaimsFactory = claimsFactory;
            Options = optionsAccessor.Options;
        }

        public UserManager<TUser> UserManager { get; private set; }
        public IAuthenticationManager AuthenticationManager { get; private set; }
        public IClaimsIdentityFactory<TUser> ClaimsFactory { get; private set; }
        public IdentityOptions Options { get; private set; }

        // Should this be a func?
        public virtual async Task<ClaimsIdentity> CreateUserIdentityAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // REVIEW: should sign in manager take options instead of using the user manager instance?
            return await ClaimsFactory.CreateAsync(user, Options.ClaimsIdentity);
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

        public virtual async Task SignInAsync(TUser user, bool isPersistent,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var userIdentity = await CreateUserIdentityAsync(user);
            AuthenticationManager.SignIn(userIdentity, isPersistent);
        }

        // TODO: Should this be async?
        public virtual void SignOut()
        {
            // REVIEW: need a new home for this option config?
            AuthenticationManager.SignOut(Options.ClaimsIdentity.AuthenticationType);
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

        public virtual async Task<bool> SendTwoFactorCodeAsync(string provider,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var userId = await AuthenticationManager.RetrieveUserId();
            if (userId == null)
            {
                return false;
            }

            var user = await UserManager.FindByIdAsync(userId, cancellationToken);
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
            return await AuthenticationManager.IsClientRememeberedAsync(userId, cancellationToken);
        }

        public virtual async Task RememberTwoFactorClient(TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var userId = await UserManager.GetUserIdAsync(user, cancellationToken);
            AuthenticationManager.RememberClient(userId);
        }

        public virtual Task ForgetTwoFactorClientAsync()
        {
            AuthenticationManager.ForgetClient();
            return Task.FromResult(0);
        }

        public virtual async Task<SignInStatus> TwoFactorSignInAsync(string provider, string code, bool isPersistent,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var userId = await AuthenticationManager.RetrieveUserId();
            if (userId == null)
            {
                return SignInStatus.Failure;
            }
            var user = await UserManager.FindByIdAsync(userId, cancellationToken);
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
                await SignInAsync(user, isPersistent);
                return SignInStatus.Success;
            }
            // If the token is incorrect, record the failure which also may cause the user to be locked out
            await UserManager.AccessFailedAsync(user, cancellationToken);
            return SignInStatus.Failure;
        }

        public async Task<SignInStatus> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent,
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
            return await SignInOrTwoFactorAsync(user, isPersistent, cancellationToken);
        }

        private async Task<SignInStatus> SignInOrTwoFactorAsync(TUser user, bool isPersistent,
            CancellationToken cancellationToken)
        {
            if (UserManager.SupportsUserTwoFactor && await UserManager.GetTwoFactorEnabledAsync(user))
            {
                if (!await IsTwoFactorClientRememberedAsync(user, cancellationToken))
                {
                    // Store the userId for use after two factor check
                    var userId = await UserManager.GetUserIdAsync(user, cancellationToken);
                    await AuthenticationManager.StoreUserId(userId);
                    return SignInStatus.RequiresVerification;
                }
            }
            await SignInAsync(user, isPersistent, cancellationToken);
            return SignInStatus.Success;
        }
    }
}