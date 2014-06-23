// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Interface that manages SignIn operations for a user
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class SignInManager<TUser> where TUser : class
    {
        public SignInManager(UserManager<TUser> userManager, IAuthenticationManager authenticationManager, 
            IClaimsIdentityFactory<TUser> claimsFactory)
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
            UserManager = userManager;
            AuthenticationManager = authenticationManager;
            ClaimsFactory = claimsFactory;
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie;
        }

        // TODO: this should go into some kind of Options/setup
        public string AuthenticationType { get; set; }

        public UserManager<TUser> UserManager { get; private set; }
        public IAuthenticationManager AuthenticationManager { get; private set; }
        public IClaimsIdentityFactory<TUser> ClaimsFactory { get; private set; }

        // Should this be a func?
        public virtual async Task<ClaimsIdentity> CreateUserIdentityAsync(TUser user)
        {
            return await ClaimsFactory.CreateAsync(user, AuthenticationType);
        }

        public virtual async Task SignInAsync(TUser user, bool isPersistent)
        {
            var userIdentity = await CreateUserIdentityAsync(user);
            AuthenticationManager.SignIn(userIdentity, isPersistent);
        }

        // TODO: Should this be async?
        public void SignOut()
        {
            AuthenticationManager.SignOut(AuthenticationType);
        }

        public virtual async Task<SignInStatus> PasswordSignInAsync(string userName, string password, 
            bool isPersistent, bool shouldLockout)
        {
            var user = await UserManager.FindByNameAsync(userName);
            if (user == null)
            {
                return SignInStatus.Failure;
            }
            if (UserManager.SupportsUserLockout && await UserManager.IsLockedOutAsync(user))
            {
                return SignInStatus.LockedOut;
            }
            if (await UserManager.CheckPasswordAsync(user, password))
            {
                return await SignInOrTwoFactor(user, isPersistent);
            }
            if (UserManager.SupportsUserLockout && shouldLockout)
            {
                // If lockout is requested, increment access failed count which might lock out the user
                await UserManager.AccessFailedAsync(user);
                if (await UserManager.IsLockedOutAsync(user))
                {
                    return SignInStatus.LockedOut;
                }
            }
            return SignInStatus.Failure;
        }

        public virtual async Task<bool> SendTwoFactorCode(string provider)
        {
            var userId = await AuthenticationManager.RetrieveUserId();
            if (userId == null)
            {
                return false;
            }

            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }
            var token = await UserManager.GenerateTwoFactorTokenAsync(user, provider);
            // See IdentityConfig.cs to plug in Email/SMS services to actually send the code
            await UserManager.NotifyTwoFactorTokenAsync(user, provider, token);
            return true;
        }

        //public async Task<bool> HasBeenVerified()
        //{
        //    return await GetVerifiedUserId() != null;
        //}

        public virtual async Task RememberTwoFactorClient(TUser user)
        {
            var userId = await UserManager.GetUserIdAsync(user);
            AuthenticationManager.RememberClient(userId);
        }

        public virtual Task ForgetTwoFactorClientAsync()
        {
            AuthenticationManager.ForgetClient();
            return Task.FromResult(0);
        }

        public virtual async Task<SignInStatus> TwoFactorSignInAsync(string provider, string code, bool isPersistent)
        {
            var userId = await AuthenticationManager.RetrieveUserId();
            if (userId == null)
            {
                return SignInStatus.Failure;
            }
            var user = await UserManager.FindByIdAsync(userId);
            if (user == null)
            {
                return SignInStatus.Failure;
            }
            if (await UserManager.IsLockedOutAsync(user))
            {
                return SignInStatus.LockedOut;
            }
            if (await UserManager.VerifyTwoFactorTokenAsync(user, provider, code))
            {
                // When token is verified correctly, clear the access failed count used for lockout
                await UserManager.ResetAccessFailedCountAsync(user);
                await SignInAsync(user, isPersistent);
                return SignInStatus.Success;
            }
            // If the token is incorrect, record the failure which also may cause the user to be locked out
            await UserManager.AccessFailedAsync(user);
            return SignInStatus.Failure;
        }

        public async Task<SignInStatus> ExternalLoginSignInAsync(UserLoginInfo loginInfo, bool isPersistent)
        {
            var user = await UserManager.FindByLoginAsync(loginInfo);
            if (user == null)
            {
                return SignInStatus.Failure;
            }
            if (await UserManager.IsLockedOutAsync(user))
            {
                return SignInStatus.LockedOut;
            }
            return await SignInOrTwoFactor(user, isPersistent);
        }

        private async Task<SignInStatus> SignInOrTwoFactor(TUser user, bool isPersistent)
        {
            if (UserManager.SupportsUserTwoFactor && await UserManager.GetTwoFactorEnabledAsync(user))
            {
                var userId = await UserManager.GetUserIdAsync(user);
                if (!await AuthenticationManager.IsClientRememeberedAsync(userId))
                {
                    // Store the userId for use after two factor check
                    await AuthenticationManager.StoreUserId(userId);
                    return SignInStatus.RequiresVerification;
                }
            }
            await SignInAsync(user, isPersistent);
            return SignInStatus.Success;
        }
    }
}