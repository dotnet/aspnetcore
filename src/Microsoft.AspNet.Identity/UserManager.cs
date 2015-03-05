// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Exposes user related api which will automatically save changes to the UserStore
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class UserManager<TUser> : IDisposable where TUser : class
    {
        private readonly Dictionary<string, IUserTokenProvider<TUser>> _tokenProviders =
            new Dictionary<string, IUserTokenProvider<TUser>>();

        private TimeSpan _defaultLockout = TimeSpan.Zero;
        private bool _disposed;
        private HttpContext _context;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="store"></param>
        /// <param name="optionsAccessor"></param>
        /// <param name="passwordHasher"></param>
        /// <param name="userValidators"></param>
        /// <param name="passwordValidators"></param>
        /// <param name="keyNormalizer"></param>
        /// <param name="errors"></param>
        /// <param name="tokenProviders"></param>
        /// <param name="loggerFactory"></param>
        public UserManager(IUserStore<TUser> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<TUser> passwordHasher,
            IEnumerable<IUserValidator<TUser>> userValidators,
            IEnumerable<IPasswordValidator<TUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IEnumerable<IUserTokenProvider<TUser>> tokenProviders,
            ILogger<UserManager<TUser>> logger,
            IHttpContextAccessor contextAccessor)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            Store = store;
            Options = optionsAccessor?.Options ?? new IdentityOptions();
            _context = contextAccessor?.HttpContext;
            PasswordHasher = passwordHasher;
            KeyNormalizer = keyNormalizer;
            ErrorDescriber = errors;

            if (userValidators != null)
            {
                foreach (var v in userValidators)
                {
                    UserValidators.Add(v);
                }
            }
            if (passwordValidators != null)
            {
                foreach (var v in passwordValidators)
                {
                    PasswordValidators.Add(v);
                }
            }

            Logger = logger ?? new Logger<UserManager<TUser>>(new LoggerFactory());

            if (tokenProviders != null)
            {
                foreach (var tokenProvider in tokenProviders)
                {
                    RegisterTokenProvider(tokenProvider);
                }
            }
        }

        /// <summary>
        ///     Persistence abstraction that the Manager operates against
        /// </summary>
        protected internal IUserStore<TUser> Store { get; set; }

        internal IPasswordHasher<TUser> PasswordHasher { get; set; }

        internal IList<IUserValidator<TUser>> UserValidators { get; } = new List<IUserValidator<TUser>>();

        internal IList<IPasswordValidator<TUser>> PasswordValidators { get; } = new List<IPasswordValidator<TUser>>();

        internal ILookupNormalizer KeyNormalizer { get; set; }

        internal IdentityErrorDescriber ErrorDescriber { get; set; }

        internal ILogger<UserManager<TUser>> Logger { get; set; }

        internal IdentityOptions Options { get; set; }

        /// <summary>
        ///     Returns true if the store is an IUserTwoFactorStore
        /// </summary>
        public virtual bool SupportsUserTwoFactor
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserTwoFactorStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserPasswordStore
        /// </summary>
        public virtual bool SupportsUserPassword
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserPasswordStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserSecurityStore
        /// </summary>
        public virtual bool SupportsUserSecurityStamp
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserSecurityStampStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserRoleStore
        /// </summary>
        public virtual bool SupportsUserRole
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserRoleStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserLoginStore
        /// </summary>
        public virtual bool SupportsUserLogin
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserLoginStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserEmailStore
        /// </summary>
        public virtual bool SupportsUserEmail
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserEmailStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserPhoneNumberStore
        /// </summary>
        public virtual bool SupportsUserPhoneNumber
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserPhoneNumberStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserClaimStore
        /// </summary>
        public virtual bool SupportsUserClaim
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserClaimStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserLockoutStore
        /// </summary>
        public virtual bool SupportsUserLockout
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserLockoutStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IQueryableUserStore
        /// </summary>
        public virtual bool SupportsQueryableUsers
        {
            get
            {
                ThrowIfDisposed();
                return Store is IQueryableUserStore<TUser>;
            }
        }

        /// <summary>
        ///     Returns an IQueryable of users if the store is an IQueryableUserStore
        /// </summary>
        public virtual IQueryable<TUser> Users
        {
            get
            {
                var queryableStore = Store as IQueryableUserStore<TUser>;
                if (queryableStore == null)
                {
                    throw new NotSupportedException(Resources.StoreNotIQueryableUserStore);
                }
                return queryableStore.Users;
            }
        }

        private CancellationToken CancellationToken
        {
            get
            {
                return _context?.RequestAborted ?? CancellationToken.None; 
            }
        }

        /// <summary>
        ///     Dispose the store context
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<IdentityResult> ValidateUserInternal(TUser user)
        {
            var errors = new List<IdentityError>();
            foreach (var v in UserValidators)
            {
                var result = await v.ValidateAsync(this, user);
                if (!result.Succeeded)
                {
                    errors.AddRange(result.Errors);
                }
            }
            return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }

        private async Task<IdentityResult> ValidatePasswordInternal(TUser user, string password)
        {
            var errors = new List<IdentityError>();
            foreach (var v in PasswordValidators)
            {
                var result = await v.ValidateAsync(this, user, password);
                if (!result.Succeeded)
                {
                    errors.AddRange(result.Errors);
                }
            }
            return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }

        public virtual Task<string> GenerateConcurrencyStampAsync(TUser user)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        /// <summary>
        ///     Validate user and update. Called by other UserManager methods
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task<IdentityResult> UpdateUserAsync(TUser user)
        {
            var result = await ValidateUserInternal(user);
            if (!result.Succeeded)
            {
                return result;
            }
            await UpdateNormalizedUserNameAsync(user);
            await UpdateNormalizedEmailAsync(user);
            return await Store.UpdateAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Create a user with no password
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> CreateAsync(TUser user)
        {
            ThrowIfDisposed();
            await UpdateSecurityStampInternal(user);
            var result = await ValidateUserInternal(user);
            if (!result.Succeeded)
            {
                return result;
            }
            if (Options.Lockout.EnabledByDefault && SupportsUserLockout)
            {
                await GetUserLockoutStore().SetLockoutEnabledAsync(user, true, CancellationToken);
            }
            await UpdateNormalizedUserNameAsync(user);
            await UpdateNormalizedEmailAsync(user);
            return await LogResultAsync(await Store.CreateAsync(user, CancellationToken), user);
        }

        /// <summary>
        ///     Update a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> UpdateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Delete a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> DeleteAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await LogResultAsync(await Store.DeleteAsync(user, CancellationToken), user);
        }

        /// <summary>
        ///     Find a user by id
        /// </summary>
        /// <param name="userId"></param>

        /// <returns></returns>
        public virtual Task<TUser> FindByIdAsync(string userId)
        {
            ThrowIfDisposed();
            return Store.FindByIdAsync(userId, CancellationToken);
        }

        /// <summary>
        ///     Find a user by name
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindByNameAsync(string userName)
        {
            ThrowIfDisposed();
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            userName = NormalizeKey(userName);
            return Store.FindByNameAsync(userName, CancellationToken);
        }

        // IUserPasswordStore methods
        private IUserPasswordStore<TUser> GetPasswordStore()
        {
            var cast = Store as IUserPasswordStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserPasswordStore);
            }
            return cast;
        }

        /// <summary>
        ///     Create a user and associates it with the given password
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> CreateAsync(TUser user, string password)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            var result = await UpdatePasswordHash(passwordStore, user, password);
            if (!result.Succeeded)
            {
                return result;
            }
            return await CreateAsync(user);
        }

        /// <summary>
        /// Normalize a key (user name, email) for uniqueness comparisons
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public virtual string NormalizeKey(string key)
        {
            return (KeyNormalizer == null) ? key : KeyNormalizer.Normalize(key);
        }

        /// <summary>
        /// Update the user's normalized user name
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task UpdateNormalizedUserNameAsync(TUser user)
        {
            var normalizedName = NormalizeKey(await GetUserNameAsync(user));
            await Store.SetNormalizedUserNameAsync(user, normalizedName, CancellationToken);
        }

        /// <summary>
        /// Get the user's name
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GetUserNameAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await Store.GetUserNameAsync(user, CancellationToken);
        }

        /// <summary>
        /// Set the user's name
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetUserNameAsync(TUser user, string userName)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await UpdateUserName(user, userName);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        private async Task UpdateUserName(TUser user, string userName)
        {
            await Store.SetUserNameAsync(user, userName, CancellationToken);
            await UpdateNormalizedUserNameAsync(user);
        }

        /// <summary>
        /// Get the user's id
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GetUserIdAsync(TUser user)
        {
            ThrowIfDisposed();
            return await Store.GetUserIdAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Returns true if the password combination is valid for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual async Task<bool> CheckPasswordAsync(TUser user, string password)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                return false;
            }
            var result = await VerifyPasswordAsync(passwordStore, user, password);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await UpdatePasswordHash(passwordStore, user, password, validatePassword: false);
                await UpdateUserAsync(user);
            }

            return await LogResultAsync(result != PasswordVerificationResult.Failed, user);
        }

        /// <summary>
        ///     Returns true if the user has a password
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> HasPasswordAsync(TUser user)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await LogResultAsync(await passwordStore.HasPasswordAsync(user, CancellationToken), user);
        }

        /// <summary>
        ///     Add a user password only if one does not already exist
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddPasswordAsync(TUser user, string password)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var hash = await passwordStore.GetPasswordHashAsync(user, CancellationToken);
            if (hash != null)
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.UserAlreadyHasPassword()), user);
            }
            var result = await UpdatePasswordHash(passwordStore, user, password);
            if (!result.Succeeded)
            {
                return await LogResultAsync(result, user);
            }
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Change a user password
        /// </summary>
        /// <param name="user"></param>
        /// <param name="currentPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ChangePasswordAsync(TUser user, string currentPassword, string newPassword)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (await VerifyPasswordAsync(passwordStore, user, currentPassword) != PasswordVerificationResult.Failed)
            {
                var result = await UpdatePasswordHash(passwordStore, user, newPassword);
                if (!result.Succeeded)
                {
                    return await LogResultAsync(result, user);
                }
                return await LogResultAsync(await UpdateUserAsync(user), user);
            }
            return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.PasswordMismatch()), user);
        }

        /// <summary>
        ///     Remove a user's password
        /// </summary>
        /// <param name="user"></param>

        /// <returns></returns>
        public virtual async Task<IdentityResult> RemovePasswordAsync(TUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await UpdatePasswordHash(passwordStore, user, null, validatePassword: false);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        internal async Task<IdentityResult> UpdatePasswordHash(IUserPasswordStore<TUser> passwordStore,
            TUser user, string newPassword, bool validatePassword = true)
        {
            if (validatePassword)
            {
                var validate = await ValidatePasswordInternal(user, newPassword);
                if (!validate.Succeeded)
                {
                    return validate;
                }
            }
            var hash = newPassword != null ? PasswordHasher.HashPassword(user, newPassword) : null;
            await passwordStore.SetPasswordHashAsync(user, hash, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return IdentityResult.Success;
        }

        /// <summary>
        /// By default, retrieves the hashed password from the user store and calls PasswordHasher.VerifyHashPassword
        /// </summary>
        /// <param name="store"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual async Task<PasswordVerificationResult> VerifyPasswordAsync(IUserPasswordStore<TUser> store, TUser user, string password)
        {
            var hash = await store.GetPasswordHashAsync(user, CancellationToken);
            return PasswordHasher.VerifyHashedPassword(user, hash, password);
        }

        // IUserSecurityStampStore methods
        private IUserSecurityStampStore<TUser> GetSecurityStore()
        {
            var cast = Store as IUserSecurityStampStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserSecurityStampStore);
            }
            return cast;
        }

        /// <summary>
        ///     Returns the current security stamp for a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GetSecurityStampAsync(TUser user)
        {
            ThrowIfDisposed();
            var securityStore = GetSecurityStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await securityStore.GetSecurityStampAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Generate a new security stamp for a user, used for SignOutEverywhere functionality
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> UpdateSecurityStampAsync(TUser user)
        {
            ThrowIfDisposed();
            GetSecurityStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await UpdateSecurityStampInternal(user);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     GenerateAsync a password reset token for the user using the UserTokenProvider
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GeneratePasswordResetTokenAsync(TUser user)
        {
            ThrowIfDisposed();
            var token = await GenerateUserTokenAsync(user, Options.PasswordResetTokenProvider, "ResetPassword");
            await LogResultAsync(IdentityResult.Success, user);
            return token;
        }

        /// <summary>
        ///     Reset a user's password using a reset password token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ResetPasswordAsync(TUser user, string token, string newPassword)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            // Make sure the token is valid and the stamp matches
            if (!await VerifyUserTokenAsync(user, Options.PasswordResetTokenProvider, "ResetPassword", token))
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.InvalidToken()), user);
            }
            var passwordStore = GetPasswordStore();
            var result = await UpdatePasswordHash(passwordStore, user, newPassword);
            if (!result.Succeeded)
            {
                return await LogResultAsync(result, user);
            }
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        // Update the security stamp if the store supports it
        internal async Task UpdateSecurityStampInternal(TUser user)
        {
            if (SupportsUserSecurityStamp)
            {
                await GetSecurityStore().SetSecurityStampAsync(user, NewSecurityStamp(), CancellationToken);
            }
        }

        private static string NewSecurityStamp()
        {
            return Guid.NewGuid().ToString();
        }

        // IUserLoginStore methods
        private IUserLoginStore<TUser> GetLoginStore()
        {
            var cast = Store as IUserLoginStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserLoginStore);
            }
            return cast;
        }

        /// <summary>
        /// Returns the user associated with this login
        /// </summary>
        /// <param name="loginProvider"></param>
        /// <param name="providerKey"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindByLoginAsync(string loginProvider, string providerKey)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (loginProvider == null)
            {
                throw new ArgumentNullException("loginProvider");
            }
            if (providerKey == null)
            {
                throw new ArgumentNullException("providerKey");
            }
            return loginStore.FindByLoginAsync(loginProvider, providerKey, CancellationToken);
        }

        /// <summary>
        ///     Remove a user login
        /// </summary>
        /// <param name="user"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveLoginAsync(TUser user, string loginProvider, string providerKey)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (loginProvider == null)
            {
                throw new ArgumentNullException("loginProvider");
            }
            if (providerKey == null)
            {
                throw new ArgumentNullException("providerKey");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await loginStore.RemoveLoginAsync(user, loginProvider, providerKey, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Associate a login with a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddLoginAsync(TUser user, UserLoginInfo login)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var existingUser = await FindByLoginAsync(login.LoginProvider, login.ProviderKey);
            if (existingUser != null)
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.LoginAlreadyAssociated()), user);
            }
            await loginStore.AddLoginAsync(user, login, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Gets the logins for a user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await loginStore.GetLoginsAsync(user, CancellationToken);
        }

        // IUserClaimStore methods
        private IUserClaimStore<TUser> GetClaimStore()
        {
            var cast = Store as IUserClaimStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserClaimStore);
            }
            return cast;
        }

        /// <summary>
        ///     Add a user claim
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claim"></param>
        /// <returns></returns>
        public virtual Task<IdentityResult> AddClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return AddClaimsAsync(user, new Claim[] { claim });
        }

        /// <summary>
        ///     Add claims for a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddClaimsAsync(TUser user, IEnumerable<Claim> claims)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claims == null)
            {
                throw new ArgumentNullException("claims");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await claimStore.AddClaimsAsync(user, claims, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Updates the give claim information with the given new claim information
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claim"></param>
        /// <param name="newClaim"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException("newClaim");
            }
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await claimStore.ReplaceClaimAsync(user, claim, newClaim, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Remove a user claim
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claim"></param>
        /// <returns></returns>
        public virtual Task<IdentityResult> RemoveClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            return RemoveClaimsAsync(user, new Claim[] { claim });
        }

        /// <summary>
        ///     Remove a user claim
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (claims == null)
            {
                throw new ArgumentNullException("claims");
            }
            await claimStore.RemoveClaimsAsync(user, claims, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Get a users's claims
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await claimStore.GetClaimsAsync(user, CancellationToken);
        }

        private IUserRoleStore<TUser> GetUserRoleStore()
        {
            var cast = Store as IUserRoleStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserRoleStore);
            }
            return cast;
        }

        /// <summary>
        ///     Add a user to a role
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddToRoleAsync(TUser user, string role)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var userRoles = await userRoleStore.GetRolesAsync(user, CancellationToken);
            if (userRoles.Contains(role))
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.UserAlreadyInRole(role)), user);
            }
            await userRoleStore.AddToRoleAsync(user, role, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Add a user to roles
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddToRolesAsync(TUser user, IEnumerable<string> roles)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (roles == null)
            {
                throw new ArgumentNullException("roles");
            }
            var userRoles = await userRoleStore.GetRolesAsync(user, CancellationToken);
            foreach (var role in roles)
            {
                if (userRoles.Contains(role))
                {
                    return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.UserAlreadyInRole(role)), user);
                }
                await userRoleStore.AddToRoleAsync(user, role, CancellationToken);
            }
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Remove a user from a role.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveFromRoleAsync(TUser user, string role)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (!await userRoleStore.IsInRoleAsync(user, role, CancellationToken))
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.UserNotInRole(role)), user);
            }
            await userRoleStore.RemoveFromRoleAsync(user, role, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Remove a user from a specified roles.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveFromRolesAsync(TUser user, IEnumerable<string> roles)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (roles == null)
            {
                throw new ArgumentNullException("roles");
            }
            foreach (var role in roles)
            {
                if (!await userRoleStore.IsInRoleAsync(user, role, CancellationToken))
                {
                    return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.UserNotInRole(role)), user);
                }
                await userRoleStore.RemoveFromRoleAsync(user, role, CancellationToken);
            }
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Returns the roles for the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IList<string>> GetRolesAsync(TUser user)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await userRoleStore.GetRolesAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Returns true if the user is in the specified role
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsInRoleAsync(TUser user, string role)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await userRoleStore.IsInRoleAsync(user, role, CancellationToken);
        }

        // IUserEmailStore methods
        internal IUserEmailStore<TUser> GetEmailStore(bool throwOnFail = true)
        {
            var cast = Store as IUserEmailStore<TUser>;
            if (throwOnFail && cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserEmailStore);
            }
            return cast;
        }

        /// <summary>
        ///     Get a user's email
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GetEmailAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetEmailAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Set a user's email
        /// </summary>
        /// <param name="user"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetEmailAsync(TUser user, string email)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await store.SetEmailAsync(user, email, CancellationToken);
            await store.SetEmailConfirmedAsync(user, false, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     FindByLoginAsync a user by his email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindByEmailAsync(string email)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (email == null)
            {
                throw new ArgumentNullException("email");
            }
            return store.FindByEmailAsync(NormalizeKey(email), CancellationToken);
        }

        /// <summary>
        /// Update the user's normalized email
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task UpdateNormalizedEmailAsync(TUser user)
        {
            var store = GetEmailStore(throwOnFail: false);
            if (store != null)
            {
                var email = await GetEmailAsync(user);
                await store.SetNormalizedEmailAsync(user, NormalizeKey(email), CancellationToken);
            }
        }


        /// <summary>
        ///     Get the confirmation token for the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async virtual Task<string> GenerateEmailConfirmationTokenAsync(TUser user)
        {
            ThrowIfDisposed();
            var token = await GenerateUserTokenAsync(user, Options.EmailConfirmationTokenProvider, "Confirmation");
            await LogResultAsync(IdentityResult.Success, user);
            return token;
        }

        /// <summary>
        ///     Confirm the user with confirmation token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ConfirmEmailAsync(TUser user, string token)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (!await VerifyUserTokenAsync(user, Options.EmailConfirmationTokenProvider, "Confirmation", token))
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.InvalidToken()), user);
            }
            await store.SetEmailConfirmedAsync(user, true, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Returns true if the user's email has been confirmed
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsEmailConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetEmailConfirmedAsync(user, CancellationToken);
        }

        private static string GetChangeEmailPurpose(string newEmail)
        {
            return "ChangeEmail:" + newEmail;
        }

        /// <summary>
        ///     Generate a change email token for the user using the UserTokenProvider
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateChangeEmailTokenAsync(TUser user, string newEmail)
        {
            ThrowIfDisposed();
            var token = await GenerateUserTokenAsync(user, Options.ChangeEmailTokenProvider, GetChangeEmailPurpose(newEmail));
            await LogResultAsync(IdentityResult.Success, user);
            return token;
        }

        /// <summary>
        ///     Change a user's email using a change email token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ChangeEmailAsync(TUser user, string newEmail, string token)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            // Make sure the token is valid and the stamp matches
            if (!await VerifyUserTokenAsync(user, Options.ChangeEmailTokenProvider, GetChangeEmailPurpose(newEmail), token))
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.InvalidToken()), user);
            }
            var store = GetEmailStore();
            await store.SetEmailAsync(user, newEmail, CancellationToken);
            await store.SetEmailConfirmedAsync(user, true, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        // IUserPhoneNumberStore methods
        internal IUserPhoneNumberStore<TUser> GetPhoneNumberStore()
        {
            var cast = Store as IUserPhoneNumberStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserPhoneNumberStore);
            }
            return cast;
        }

        /// <summary>
        ///     Get a user's phoneNumber
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GetPhoneNumberAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetPhoneNumberAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Set a user's phoneNumber
        /// </summary>
        /// <param name="user"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await store.SetPhoneNumberAsync(user, phoneNumber, CancellationToken);
            await store.SetPhoneNumberConfirmedAsync(user, false, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Set a user's phoneNumber with the verification token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ChangePhoneNumberAsync(TUser user, string phoneNumber, string token)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (!await VerifyChangePhoneNumberTokenAsync(user, token, phoneNumber))
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.InvalidToken()), user);
            }
            await store.SetPhoneNumberAsync(user, phoneNumber, CancellationToken);
            await store.SetPhoneNumberConfirmedAsync(user, true, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Returns true if the user's phone number has been confirmed
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsPhoneNumberConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetPhoneNumberConfirmedAsync(user, CancellationToken);
        }

        // Two factor APIS
        internal async Task<SecurityToken> CreateSecurityTokenAsync(TUser user)
        {
            return new SecurityToken(Encoding.Unicode.GetBytes(await GetSecurityStampAsync(user)));
        }

        /// <summary>
        ///     Get a phone number code for a user and phone number
        /// </summary>
        /// <param name="user"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateChangePhoneNumberTokenAsync(TUser user, string phoneNumber)
        {
            ThrowIfDisposed();
            var token = Rfc6238AuthenticationService.GenerateCode(
                await CreateSecurityTokenAsync(user), phoneNumber)
                   .ToString(CultureInfo.InvariantCulture);
            await LogResultAsync(IdentityResult.Success, user);
            return token;
        }

        /// <summary>
        ///     Verify a phone number code for a specific user and phone number
        /// </summary>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public virtual async Task<bool> VerifyChangePhoneNumberTokenAsync(TUser user, string token, string phoneNumber)
        {
            ThrowIfDisposed();
            var securityToken = await CreateSecurityTokenAsync(user);
            int code;
            if (securityToken != null && Int32.TryParse(token, out code))
            {
                if (Rfc6238AuthenticationService.ValidateCode(securityToken, code, phoneNumber))
                {
                    await LogResultAsync(IdentityResult.Success, user);
                    return true;
                }
            }
            await LogResultAsync(IdentityResult.Failed(ErrorDescriber.InvalidToken()), user);
            return false;
        }

        /// <summary>
        ///     Verify a user token with the specified purpose
        /// </summary>
        /// <param name="user"></param>
        /// <param name="purpose"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<bool> VerifyUserTokenAsync(TUser user, string tokenProvider, string purpose, string token)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (tokenProvider == null)
            {
                throw new ArgumentNullException(nameof(tokenProvider));
            }
            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.NoTokenProvider, tokenProvider));
            }
            // Make sure the token is valid
            var result = await _tokenProviders[tokenProvider].ValidateAsync(purpose, token, this, user);

            if (result)
            {
                await LogResultAsync(IdentityResult.Success, user);
            }
            else
            {
                await LogResultAsync(IdentityResult.Failed(ErrorDescriber.InvalidToken()), user);
            }

            return result;
        }

        /// <summary>
        ///     Get a user token for a specific purpose
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateUserTokenAsync(TUser user, string tokenProvider, string purpose)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (tokenProvider == null)
            {
                throw new ArgumentNullException(nameof(tokenProvider));
            }
            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.NoTokenProvider, tokenProvider));
            }

            var token = await _tokenProviders[tokenProvider].GenerateAsync(purpose, this, user);
            await LogResultAsync(IdentityResult.Success, user);
            return token;
        }

        /// <summary>
        ///     Register a user token provider
        /// </summary>
        /// <param name="provider"></param>
        public virtual void RegisterTokenProvider(IUserTokenProvider<TUser> provider)
        {
            ThrowIfDisposed();
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            _tokenProviders[provider.Name] = provider;
        }

        /// <summary>
        ///     Returns a list of valid two factor providers for a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IList<string>> GetValidTwoFactorProvidersAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var results = new List<string>();
            foreach (var f in _tokenProviders)
            {
                if (await f.Value.CanGenerateTwoFactorTokenAsync(this, user))
                {
                    results.Add(f.Key);
                }
            }
            return results;
        }

        /// <summary>
        ///     Verify a user token with the specified provider
        /// </summary>
        /// <param name="user"></param>
        /// <param name="twoFactorProvider"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async Task<bool> VerifyTwoFactorTokenAsync(TUser user, string tokenProvider, string token)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture,
                    Resources.NoTokenProvider, tokenProvider));
            }
            // Make sure the token is valid
            var result = await _tokenProviders[tokenProvider].ValidateAsync("TwoFactor", token, this, user);
            if (result)
            {
                await LogResultAsync(IdentityResult.Success, user);
            }
            else
            {
                await LogResultAsync(IdentityResult.Failed(ErrorDescriber.InvalidToken()), user);
            }
            return result;
        }

        /// <summary>
        ///     Get a user token for a specific user factor provider
        /// </summary>
        /// <param name="user"></param>
        /// <param name="twoFactorProvider"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateTwoFactorTokenAsync(TUser user, string tokenProvider)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture,
                    Resources.NoTokenProvider, tokenProvider));
            }
            var token = await _tokenProviders[tokenProvider].GenerateAsync("TwoFactor", this, user);
            await LogResultAsync(IdentityResult.Success, user);
            return token;
        }

        // IUserFactorStore methods
        internal IUserTwoFactorStore<TUser> GetUserTwoFactorStore()
        {
            var cast = Store as IUserTwoFactorStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserTwoFactorStore);
            }
            return cast;
        }

        /// <summary>
        ///     Get a user's two factor provider
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserTwoFactorStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetTwoFactorEnabledAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Set whether a user has two factor enabled or not
        /// </summary>
        /// <param name="user"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();
            var store = GetUserTwoFactorStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await store.SetTwoFactorEnabledAsync(user, enabled, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        // IUserLockoutStore methods
        internal IUserLockoutStore<TUser> GetUserLockoutStore()
        {
            var cast = Store as IUserLockoutStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserLockoutStore);
            }
            return cast;
        }

        /// <summary>
        ///     Returns true if the user is locked out
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsLockedOutAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (!await store.GetLockoutEnabledAsync(user, CancellationToken))
            {
                return false;
            }
            var lockoutTime = await store.GetLockoutEndDateAsync(user, CancellationToken);
            return lockoutTime >= DateTimeOffset.UtcNow;
        }

        /// <summary>
        ///     Sets whether the user allows lockout
        /// </summary>
        /// <param name="user"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await store.SetLockoutEnabledAsync(user, enabled, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Returns whether the user allows lockout
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetLockoutEnabledAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Returns the user lockout end date
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetLockoutEndDateAsync(user, CancellationToken);
        }

        /// <summary>
        ///     Sets the user lockout end date
        /// </summary>
        /// <param name="user"></param>
        /// <param name="lockoutEnd"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (!await store.GetLockoutEnabledAsync(user, CancellationToken))
            {
                return await LogResultAsync(IdentityResult.Failed(ErrorDescriber.UserLockoutNotEnabled()), user);
            }
            await store.SetLockoutEndDateAsync(user, lockoutEnd, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        /// Increments the access failed count for the user and if the failed access account is greater than or equal
        /// to the MaxFailedAccessAttempsBeforeLockout, the user will be locked out for the next
        /// DefaultAccountLockoutTimeSpan and the AccessFailedCount will be reset to 0.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AccessFailedAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            // If this puts the user over the threshold for lockout, lock them out and reset the access failed count
            var count = await store.IncrementAccessFailedCountAsync(user, CancellationToken);
            if (count < Options.Lockout.MaxFailedAccessAttempts)
            {
                return await LogResultAsync(await UpdateUserAsync(user), user);
            }
            await store.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.Add(Options.Lockout.DefaultLockoutTimeSpan),
                CancellationToken);
            await store.ResetAccessFailedCountAsync(user, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Resets the access failed count for the user to 0
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ResetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await store.ResetAccessFailedCountAsync(user, CancellationToken);
            return await LogResultAsync(await UpdateUserAsync(user), user);
        }

        /// <summary>
        ///     Returns the number of failed access attempts for the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task<int> GetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return await store.GetAccessFailedCountAsync(user, CancellationToken);
        }

        public virtual Task<IList<TUser>> GetUsersForClaimAsync(Claim claim)
        {
            ThrowIfDisposed();
            var store = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            return store.GetUsersForClaimAsync(claim, CancellationToken);
        }

        /// <summary>
        ///     Get all the users in a role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual Task<IList<TUser>> GetUsersInRoleAsync(string roleName)
        {
            ThrowIfDisposed();
            var store = GetUserRoleStore();
            if (roleName == null)
            {
                throw new ArgumentNullException("role");
            }

            return store.GetUsersInRoleAsync(roleName, CancellationToken);
        }

        /// <summary>
        ///     Logs the current Identity Result and returns result object
        /// </summary>
        /// <param name="result"></param>
        /// <param name="user"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        protected async Task<IdentityResult> LogResultAsync(IdentityResult result,
            TUser user, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            result.Log(Logger, Resources.FormatLoggingResultMessage(methodName, await GetUserIdAsync(user)));
            return result;
        }

        /// <summary>
        ///     Logs result of operation being true/false
        /// </summary>
        /// <param name="result"></param>
        /// <param name="user"></param>
        /// <param name="methodName"></param>
        /// <returns>result</returns>
        protected async Task<bool> LogResultAsync(bool result,
            TUser user, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var baseMessage = Resources.FormatLoggingResultMessage(methodName, await GetUserIdAsync(user));
            if (result)
            {
                Logger.LogInformation(string.Format("{0} : {1}", baseMessage, result.ToString()));
            }
            else
            {
                Logger.LogWarning(string.Format("{0} : {1}", baseMessage, result.ToString()));
            }

            return result;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        ///     When disposing, actually dipose the store context
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                Store.Dispose();
                _disposed = true;
            }
        }
    }
}