// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Identity.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Provides the APIs for managing user in a persistence store.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class UserManager<TUser> : IDisposable where TUser : class
    {
        /// <summary>
        /// The data protection purpose used for the reset password related methods.
        /// </summary>
        public const string ResetPasswordTokenPurpose = "ResetPassword";

        /// <summary>
        /// The data protection purpose used for the change phone number methods.
        /// </summary>
        public const string ChangePhoneNumberTokenPurpose = "ChangePhoneNumber";

        /// <summary>
        /// The data protection purpose used for the email confirmation related methods.
        /// </summary>
        public const string ConfirmEmailTokenPurpose = "EmailConfirmation";

        private readonly Dictionary<string, IUserTwoFactorTokenProvider<TUser>> _tokenProviders =
            new Dictionary<string, IUserTwoFactorTokenProvider<TUser>>();

        private TimeSpan _defaultLockout = TimeSpan.Zero;
        private bool _disposed;
#if NETSTANDARD2_0
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
#endif
        private IServiceProvider _services;

        /// <summary>
        /// The cancellation token used to cancel operations.
        /// </summary>
        protected virtual CancellationToken CancellationToken => CancellationToken.None;

        /// <summary>
        /// Constructs a new instance of <see cref="UserManager{TUser}"/>.
        /// </summary>
        /// <param name="store">The persistence store the manager will operate over.</param>
        /// <param name="optionsAccessor">The accessor used to access the <see cref="IdentityOptions"/>.</param>
        /// <param name="passwordHasher">The password hashing implementation to use when saving passwords.</param>
        /// <param name="userValidators">A collection of <see cref="IUserValidator{TUser}"/> to validate users against.</param>
        /// <param name="passwordValidators">A collection of <see cref="IPasswordValidator{TUser}"/> to validate passwords against.</param>
        /// <param name="keyNormalizer">The <see cref="ILookupNormalizer"/> to use when generating index keys for users.</param>
        /// <param name="errors">The <see cref="IdentityErrorDescriber"/> used to provider error messages.</param>
        /// <param name="services">The <see cref="IServiceProvider"/> used to resolve services.</param>
        /// <param name="logger">The logger used to log messages, warnings and errors.</param>
        public UserManager(IUserStore<TUser> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<TUser> passwordHasher,
            IEnumerable<IUserValidator<TUser>> userValidators,
            IEnumerable<IPasswordValidator<TUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<TUser>> logger)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            Store = store;
            Options = optionsAccessor?.Value ?? new IdentityOptions();
            PasswordHasher = passwordHasher;
            KeyNormalizer = keyNormalizer;
            ErrorDescriber = errors;
            Logger = logger;

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

            _services = services;
            if (services != null)
            {
                foreach (var providerName in Options.Tokens.ProviderMap.Keys)
                {
                    var description = Options.Tokens.ProviderMap[providerName];

                    var provider = (description.ProviderInstance ?? services.GetRequiredService(description.ProviderType))
                        as IUserTwoFactorTokenProvider<TUser>;
                    if (provider != null)
                    {
                        RegisterTokenProvider(providerName, provider);
                    }
                }
            }

            if (Options.Stores.ProtectPersonalData)
            {
                if (!(Store is IProtectedUserStore<TUser>))
                {
                    throw new InvalidOperationException(Resources.StoreNotIProtectedUserStore);
                }
                if (services.GetService<ILookupProtector>() == null)
                {
                    throw new InvalidOperationException(Resources.NoPersonalDataProtector);
                }
            }
        }

        /// <summary>
        /// Gets or sets the persistence store the manager operates over.
        /// </summary>
        /// <value>The persistence store the manager operates over.</value>
        protected internal IUserStore<TUser> Store { get; set; }

        /// <summary>
        /// The <see cref="ILogger"/> used to log messages from the manager.
        /// </summary>
        /// <value>
        /// The <see cref="ILogger"/> used to log messages from the manager.
        /// </value>
        public virtual ILogger Logger { get; set; }

        /// <summary>
        /// The <see cref="IPasswordHasher{TUser}"/> used to hash passwords.
        /// </summary>
        public IPasswordHasher<TUser> PasswordHasher { get; set; }

        /// <summary>
        /// The <see cref="IUserValidator{TUser}"/> used to validate users.
        /// </summary>
        public IList<IUserValidator<TUser>> UserValidators { get; } = new List<IUserValidator<TUser>>();

        /// <summary>
        /// The <see cref="IPasswordValidator{TUser}"/> used to validate passwords.
        /// </summary>
        public IList<IPasswordValidator<TUser>> PasswordValidators { get; } = new List<IPasswordValidator<TUser>>();

        /// <summary>
        /// The <see cref="ILookupNormalizer"/> used to normalize things like user and role names.
        /// </summary>
        public ILookupNormalizer KeyNormalizer { get; set; }

        /// <summary>
        /// The <see cref="IdentityErrorDescriber"/> used to generate error messages.
        /// </summary>
        public IdentityErrorDescriber ErrorDescriber { get; set; }

        /// <summary>
        /// The <see cref="IdentityOptions"/> used to configure Identity.
        /// </summary>
        public IdentityOptions Options { get; set; }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports authentication tokens.
        /// </summary>
        /// <value>
        /// true if the backing user store supports authentication tokens, otherwise false.
        /// </value>
        public virtual bool SupportsUserAuthenticationTokens
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserAuthenticationTokenStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports a user authenticator.
        /// </summary>
        /// <value>
        /// true if the backing user store supports a user authenticator, otherwise false.
        /// </value>
        public virtual bool SupportsUserAuthenticatorKey
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserAuthenticatorKeyStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports recovery codes.
        /// </summary>
        /// <value>
        /// true if the backing user store supports a user authenticator, otherwise false.
        /// </value>
        public virtual bool SupportsUserTwoFactorRecoveryCodes
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserTwoFactorRecoveryCodeStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports two factor authentication.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user two factor authentication, otherwise false.
        /// </value>
        public virtual bool SupportsUserTwoFactor
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserTwoFactorStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports user passwords.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user passwords, otherwise false.
        /// </value>
        public virtual bool SupportsUserPassword
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserPasswordStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports security stamps.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user security stamps, otherwise false.
        /// </value>
        public virtual bool SupportsUserSecurityStamp
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserSecurityStampStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports user roles.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user roles, otherwise false.
        /// </value>
        public virtual bool SupportsUserRole
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserRoleStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports external logins.
        /// </summary>
        /// <value>
        /// true if the backing user store supports external logins, otherwise false.
        /// </value>
        public virtual bool SupportsUserLogin
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserLoginStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports user emails.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user emails, otherwise false.
        /// </value>
        public virtual bool SupportsUserEmail
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserEmailStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports user telephone numbers.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user telephone numbers, otherwise false.
        /// </value>
        public virtual bool SupportsUserPhoneNumber
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserPhoneNumberStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports user claims.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user claims, otherwise false.
        /// </value>
        public virtual bool SupportsUserClaim
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserClaimStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports user lock-outs.
        /// </summary>
        /// <value>
        /// true if the backing user store supports user lock-outs, otherwise false.
        /// </value>
        public virtual bool SupportsUserLockout
        {
            get
            {
                ThrowIfDisposed();
                return Store is IUserLockoutStore<TUser>;
            }
        }

        /// <summary>
        /// Gets a flag indicating whether the backing user store supports returning
        /// <see cref="IQueryable"/> collections of information.
        /// </summary>
        /// <value>
        /// true if the backing user store supports returning <see cref="IQueryable"/> collections of
        /// information, otherwise false.
        /// </value>
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

        /// <summary>
        /// Releases all resources used by the user manager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the Name claim value if present otherwise returns null.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <returns>The Name claim value, or null if the claim is not present.</returns>
        /// <remarks>The Name claim is identified by <see cref="ClaimsIdentity.DefaultNameClaimType"/>.</remarks>
        public virtual string GetUserName(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            return principal.FindFirstValue(Options.ClaimsIdentity.UserNameClaimType);
        }

        /// <summary>
        /// Returns the User ID claim value if present otherwise returns null.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance.</param>
        /// <returns>The User ID claim value, or null if the claim is not present.</returns>
        /// <remarks>The User ID claim is identified by <see cref="ClaimTypes.NameIdentifier"/>.</remarks>
        public virtual string GetUserId(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            return principal.FindFirstValue(Options.ClaimsIdentity.UserIdClaimType);
        }

        /// <summary>
        /// Returns the user corresponding to the IdentityOptions.ClaimsIdentity.UserIdClaimType claim in
        /// the principal or null.
        /// </summary>
        /// <param name="principal">The principal which contains the user id claim.</param>
        /// <returns>The user corresponding to the IdentityOptions.ClaimsIdentity.UserIdClaimType claim in
        /// the principal or null</returns>
        public virtual Task<TUser> GetUserAsync(ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }
            var id = GetUserId(principal);
            return id == null ? Task.FromResult<TUser>(null) : FindByIdAsync(id);
        }

        /// <summary>
        /// Generates a value suitable for use in concurrency tracking.
        /// </summary>
        /// <param name="user">The user to generate the stamp for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the security
        /// stamp for the specified <paramref name="user"/>.
        /// </returns>
        public virtual Task<string> GenerateConcurrencyStampAsync(TUser user)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Creates the specified <paramref name="user"/> in the backing store with no password,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> CreateAsync(TUser user)
        {
            ThrowIfDisposed();
            await UpdateSecurityStampInternal(user);
            var result = await ValidateUserAsync(user);
            if (!result.Succeeded)
            {
                return result;
            }
            if (Options.Lockout.AllowedForNewUsers && SupportsUserLockout)
            {
                await GetUserLockoutStore().SetLockoutEnabledAsync(user, true, CancellationToken);
            }
            await UpdateNormalizedUserNameAsync(user);
            await UpdateNormalizedEmailAsync(user);

            return await Store.CreateAsync(user, CancellationToken);
        }

        /// <summary>
        /// Updates the specified <paramref name="user"/> in the backing store.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual Task<IdentityResult> UpdateAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return UpdateUserAsync(user);
        }

        /// <summary>
        /// Deletes the specified <paramref name="user"/> from the backing store.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual Task<IdentityResult> DeleteAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Store.DeleteAsync(user, CancellationToken);
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user ID to search for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="userId"/> if it exists.
        /// </returns>
        public virtual Task<TUser> FindByIdAsync(string userId)
        {
            ThrowIfDisposed();
            return Store.FindByIdAsync(userId, CancellationToken);
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified user name.
        /// </summary>
        /// <param name="userName">The user name to search for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="userName"/> if it exists.
        /// </returns>
        public virtual async Task<TUser> FindByNameAsync(string userName)
        {
            ThrowIfDisposed();
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }
            userName = NormalizeName(userName);

            var user = await Store.FindByNameAsync(userName, CancellationToken);

            // Need to potentially check all keys
            if (user == null && Options.Stores.ProtectPersonalData)
            {
                var keyRing = _services.GetService<ILookupProtectorKeyRing>();
                var protector = _services.GetService<ILookupProtector>();
                if (keyRing != null && protector != null)
                {
                    foreach (var key in keyRing.GetAllKeyIds())
                    {
                        var oldKey = protector.Protect(key, userName);
                        user = await Store.FindByNameAsync(oldKey, CancellationToken);
                        if (user != null)
                        {
                            return user;
                        }
                    }
                }
            }
            return user;
        }

        /// <summary>
        /// Creates the specified <paramref name="user"/> in the backing store with given password,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <param name="password">The password for the user to hash and store.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> CreateAsync(TUser user, string password)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            var result = await UpdatePasswordHash(passwordStore, user, password);
            if (!result.Succeeded)
            {
                return result;
            }
            return await CreateAsync(user);
        }

        /// <summary>
        /// Normalize user or role name for consistent comparisons.
        /// </summary>
        /// <param name="name">The name to normalize.</param>
        /// <returns>A normalized value representing the specified <paramref name="name"/>.</returns>
        public virtual string NormalizeName(string name)
            =>  (KeyNormalizer == null) ? name : KeyNormalizer.NormalizeName(name);

        /// <summary>
        /// Normalize email for consistent comparisons.
        /// </summary>
        /// <param name="email">The email to normalize.</param>
        /// <returns>A normalized value representing the specified <paramref name="email"/>.</returns>
        public virtual string NormalizeEmail(string email)
            =>  (KeyNormalizer == null) ? email : KeyNormalizer.NormalizeEmail(email);

        private string ProtectPersonalData(string data)
        {
            if (Options.Stores.ProtectPersonalData)
            {
                var keyRing = _services.GetService<ILookupProtectorKeyRing>();
                var protector = _services.GetService<ILookupProtector>();
                return protector.Protect(keyRing.CurrentKeyId, data);
            }
            return data;
        }

        /// <summary>
        /// Updates the normalized user name for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose user name should be normalized and updated.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual async Task UpdateNormalizedUserNameAsync(TUser user)
        {
            var normalizedName = NormalizeName(await GetUserNameAsync(user));
            normalizedName = ProtectPersonalData(normalizedName);
            await Store.SetNormalizedUserNameAsync(user, normalizedName, CancellationToken);
        }

        /// <summary>
        /// Gets the user name for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose name should be retrieved.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the name for the specified <paramref name="user"/>.</returns>
        public virtual async Task<string> GetUserNameAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await Store.GetUserNameAsync(user, CancellationToken);
        }

        /// <summary>
        /// Sets the given <paramref name="userName" /> for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose name should be set.</param>
        /// <param name="userName">The user name to set.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual async Task<IdentityResult> SetUserNameAsync(TUser user, string userName)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await Store.SetUserNameAsync(user, userName, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Gets the user identifier for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose identifier should be retrieved.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the identifier for the specified <paramref name="user"/>.</returns>
        public virtual async Task<string> GetUserIdAsync(TUser user)
        {
            ThrowIfDisposed();
            return await Store.GetUserIdAsync(user, CancellationToken);
        }

        /// <summary>
        /// Returns a flag indicating whether the given <paramref name="password"/> is valid for the
        /// specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose password should be validated.</param>
        /// <param name="password">The password to validate</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing true if
        /// the specified <paramref name="password" /> matches the one store for the <paramref name="user"/>,
        /// otherwise false.</returns>
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

            var success = result != PasswordVerificationResult.Failed;
            if (!success)
            {
                Logger.LogWarning(0, "Invalid password for user {userId}.", await GetUserIdAsync(user));
            }
            return success;
        }

        /// <summary>
        /// Gets a flag indicating whether the specified <paramref name="user"/> has a password.
        /// </summary>
        /// <param name="user">The user to return a flag for, indicating whether they have a password or not.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, returning true if the specified <paramref name="user"/> has a password
        /// otherwise false.
        /// </returns>
        public virtual Task<bool> HasPasswordAsync(TUser user)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return passwordStore.HasPasswordAsync(user, CancellationToken);
        }

        /// <summary>
        /// Adds the <paramref name="password"/> to the specified <paramref name="user"/> only if the user
        /// does not already have a password.
        /// </summary>
        /// <param name="user">The user whose password should be set.</param>
        /// <param name="password">The password to set.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> AddPasswordAsync(TUser user, string password)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var hash = await passwordStore.GetPasswordHashAsync(user, CancellationToken);
            if (hash != null)
            {
                Logger.LogWarning(1, "User {userId} already has a password.", await GetUserIdAsync(user));
                return IdentityResult.Failed(ErrorDescriber.UserAlreadyHasPassword());
            }
            var result = await UpdatePasswordHash(passwordStore, user, password);
            if (!result.Succeeded)
            {
                return result;
            }
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Changes a user's password after confirming the specified <paramref name="currentPassword"/> is correct,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose password should be set.</param>
        /// <param name="currentPassword">The current password to validate before changing.</param>
        /// <param name="newPassword">The new password to set for the specified <paramref name="user"/>.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> ChangePasswordAsync(TUser user, string currentPassword, string newPassword)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }


            if (await VerifyPasswordAsync(passwordStore, user, currentPassword) != PasswordVerificationResult.Failed)
            {
                var result = await UpdatePasswordHash(passwordStore, user, newPassword);
                if (!result.Succeeded)
                {
                    return result;
                }
                return await UpdateUserAsync(user);
            }
            Logger.LogWarning(2, "Change password failed for user {userId}.", await GetUserIdAsync(user));
            return IdentityResult.Failed(ErrorDescriber.PasswordMismatch());
        }

        /// <summary>
        /// Removes a user's password.
        /// </summary>
        /// <param name="user">The user whose password should be removed.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> RemovePasswordAsync(TUser user)
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await UpdatePasswordHash(passwordStore, user, null, validatePassword: false);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Returns a <see cref="PasswordVerificationResult"/> indicating the result of a password hash comparison.
        /// </summary>
        /// <param name="store">The store containing a user's password.</param>
        /// <param name="user">The user whose password should be verified.</param>
        /// <param name="password">The password to verify.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="PasswordVerificationResult"/>
        /// of the operation.
        /// </returns>
        protected virtual async Task<PasswordVerificationResult> VerifyPasswordAsync(IUserPasswordStore<TUser> store, TUser user, string password)
        {
            var hash = await store.GetPasswordHashAsync(user, CancellationToken);
            if (hash == null)
            {
                return PasswordVerificationResult.Failed;
            }
            return PasswordHasher.VerifyHashedPassword(user, hash, password);
        }

        /// <summary>
        /// Get the security stamp for the specified <paramref name="user" />.
        /// </summary>
        /// <param name="user">The user whose security stamp should be set.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the security stamp for the specified <paramref name="user"/>.</returns>
        public virtual async Task<string> GetSecurityStampAsync(TUser user)
        {
            ThrowIfDisposed();
            var securityStore = GetSecurityStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var stamp = await securityStore.GetSecurityStampAsync(user, CancellationToken);
            if (stamp == null)
            {
                Logger.LogWarning(15, "GetSecurityStampAsync for user {userId} failed because stamp was null.", await GetUserIdAsync(user));
                throw new InvalidOperationException(Resources.NullSecurityStamp);
            }
            return stamp;
        }

        /// <summary>
        /// Regenerates the security stamp for the specified <paramref name="user" />.
        /// </summary>
        /// <param name="user">The user whose security stamp should be regenerated.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        /// <remarks>
        /// Regenerating a security stamp will sign out any saved login for the user.
        /// </remarks>
        public virtual async Task<IdentityResult> UpdateSecurityStampAsync(TUser user)
        {
            ThrowIfDisposed();
            GetSecurityStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Generates a password reset token for the specified <paramref name="user"/>, using
        /// the configured password reset token provider.
        /// </summary>
        /// <param name="user">The user to generate a password reset token for.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation,
        /// containing a password reset token for the specified <paramref name="user"/>.</returns>
        public virtual Task<string> GeneratePasswordResetTokenAsync(TUser user)
        {
            ThrowIfDisposed();
            return GenerateUserTokenAsync(user, Options.Tokens.PasswordResetTokenProvider, ResetPasswordTokenPurpose);
        }

        /// <summary>
        /// Resets the <paramref name="user"/>'s password to the specified <paramref name="newPassword"/> after
        /// validating the given password reset <paramref name="token"/>.
        /// </summary>
        /// <param name="user">The user whose password should be reset.</param>
        /// <param name="token">The password reset token to verify.</param>
        /// <param name="newPassword">The new password to set if reset token verification succeeds.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> ResetPasswordAsync(TUser user, string token, string newPassword)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Make sure the token is valid and the stamp matches
            if (!await VerifyUserTokenAsync(user, Options.Tokens.PasswordResetTokenProvider, ResetPasswordTokenPurpose, token))
            {
                return IdentityResult.Failed(ErrorDescriber.InvalidToken());
            }
            var result = await UpdatePasswordHash(user, newPassword, validatePassword: true);
            if (!result.Succeeded)
            {
                return result;
            }
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Retrieves the user associated with the specified external login provider and login provider key.
        /// </summary>
        /// <param name="loginProvider">The login provider who provided the <paramref name="providerKey"/>.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing the user, if any which matched the specified login provider and key.
        /// </returns>
        public virtual Task<TUser> FindByLoginAsync(string loginProvider, string providerKey)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }
            if (providerKey == null)
            {
                throw new ArgumentNullException(nameof(providerKey));
            }
            return loginStore.FindByLoginAsync(loginProvider, providerKey, CancellationToken);
        }

        /// <summary>
        /// Attempts to remove the provided external login information from the specified <paramref name="user"/>.
        /// and returns a flag indicating whether the removal succeed or not.
        /// </summary>
        /// <param name="user">The user to remove the login information from.</param>
        /// <param name="loginProvider">The login provide whose information should be removed.</param>
        /// <param name="providerKey">The key given by the external login provider for the specified user.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> RemoveLoginAsync(TUser user, string loginProvider, string providerKey)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }
            if (providerKey == null)
            {
                throw new ArgumentNullException(nameof(providerKey));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await loginStore.RemoveLoginAsync(user, loginProvider, providerKey, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Adds an external <see cref="UserLoginInfo"/> to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the login to.</param>
        /// <param name="login">The external <see cref="UserLoginInfo"/> to add to the specified <paramref name="user"/>.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> AddLoginAsync(TUser user, UserLoginInfo login)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var existingUser = await FindByLoginAsync(login.LoginProvider, login.ProviderKey);
            if (existingUser != null)
            {
                Logger.LogWarning(4, "AddLogin for user {userId} failed because it was already associated with another user.", await GetUserIdAsync(user));
                return IdentityResult.Failed(ErrorDescriber.LoginAlreadyAssociated());
            }
            await loginStore.AddLoginAsync(user, login, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Retrieves the associated logins for the specified <param ref="user"/>.
        /// </summary>
        /// <param name="user">The user whose associated logins to retrieve.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing a list of <see cref="UserLoginInfo"/> for the specified <paramref name="user"/>, if any.
        /// </returns>
        public virtual async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await loginStore.GetLoginsAsync(user, CancellationToken);
        }

        /// <summary>
        /// Adds the specified <paramref name="claim"/> to the <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the claim to.</param>
        /// <param name="claim">The claim to add.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual Task<IdentityResult> AddClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return AddClaimsAsync(user, new Claim[] { claim });
        }

        /// <summary>
        /// Adds the specified <paramref name="claims"/> to the <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the claim to.</param>
        /// <param name="claims">The claims to add.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> AddClaimsAsync(TUser user, IEnumerable<Claim> claims)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await claimStore.AddClaimsAsync(user, claims, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Replaces the given <paramref name="claim"/> on the specified <paramref name="user"/> with the <paramref name="newClaim"/>
        /// </summary>
        /// <param name="user">The user to replace the claim on.</param>
        /// <param name="claim">The claim to replace.</param>
        /// <param name="newClaim">The new claim to replace the existing <paramref name="claim"/> with.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await claimStore.ReplaceClaimAsync(user, claim, newClaim, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Removes the specified <paramref name="claim"/> from the given <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the specified <paramref name="claim"/> from.</param>
        /// <param name="claim">The <see cref="Claim"/> to remove.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual Task<IdentityResult> RemoveClaimAsync(TUser user, Claim claim)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            return RemoveClaimsAsync(user, new Claim[] { claim });
        }

        /// <summary>
        /// Removes the specified <paramref name="claims"/> from the given <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the specified <paramref name="claims"/> from.</param>
        /// <param name="claims">A collection of <see cref="Claim"/>s to remove.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            await claimStore.RemoveClaimsAsync(user, claims, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Gets a list of <see cref="Claim"/>s to be belonging to the specified <paramref name="user"/> as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose claims to retrieve.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a list of <see cref="Claim"/>s.
        /// </returns>
        public virtual async Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await claimStore.GetClaimsAsync(user, CancellationToken);
        }

        /// <summary>
        /// Add the specified <paramref name="user"/> to the named role.
        /// </summary>
        /// <param name="user">The user to add to the named role.</param>
        /// <param name="role">The name of the role to add the user to.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> AddToRoleAsync(TUser user, string role)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var normalizedRole = NormalizeName(role);
            if (await userRoleStore.IsInRoleAsync(user, normalizedRole, CancellationToken))
            {
                return await UserAlreadyInRoleError(user, role);
            }
            await userRoleStore.AddToRoleAsync(user, normalizedRole, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Add the specified <paramref name="user"/> to the named roles.
        /// </summary>
        /// <param name="user">The user to add to the named roles.</param>
        /// <param name="roles">The name of the roles to add the user to.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> AddToRolesAsync(TUser user, IEnumerable<string> roles)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            foreach (var role in roles.Distinct())
            {
                var normalizedRole = NormalizeName(role);
                if (await userRoleStore.IsInRoleAsync(user, normalizedRole, CancellationToken))
                {
                    return await UserAlreadyInRoleError(user, role);
                }
                await userRoleStore.AddToRoleAsync(user, normalizedRole, CancellationToken);
            }
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Removes the specified <paramref name="user"/> from the named role.
        /// </summary>
        /// <param name="user">The user to remove from the named role.</param>
        /// <param name="role">The name of the role to remove the user from.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> RemoveFromRoleAsync(TUser user, string role)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var normalizedRole = NormalizeName(role);
            if (!await userRoleStore.IsInRoleAsync(user, normalizedRole, CancellationToken))
            {
                return await UserNotInRoleError(user, role);
            }
            await userRoleStore.RemoveFromRoleAsync(user, normalizedRole, CancellationToken);
            return await UpdateUserAsync(user);
        }

        private async Task<IdentityResult> UserAlreadyInRoleError(TUser user, string role)
        {
            Logger.LogWarning(5, "User {userId} is already in role {role}.", await GetUserIdAsync(user), role);
            return IdentityResult.Failed(ErrorDescriber.UserAlreadyInRole(role));
        }

        private async Task<IdentityResult> UserNotInRoleError(TUser user, string role)
        {
            Logger.LogWarning(6, "User {userId} is not in role {role}.", await GetUserIdAsync(user), role);
            return IdentityResult.Failed(ErrorDescriber.UserNotInRole(role));
        }

        /// <summary>
        /// Removes the specified <paramref name="user"/> from the named roles.
        /// </summary>
        /// <param name="user">The user to remove from the named roles.</param>
        /// <param name="roles">The name of the roles to remove the user from.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> RemoveFromRolesAsync(TUser user, IEnumerable<string> roles)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (roles == null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            foreach (var role in roles)
            {
                var normalizedRole = NormalizeName(role);
                if (!await userRoleStore.IsInRoleAsync(user, normalizedRole, CancellationToken))
                {
                    return await UserNotInRoleError(user, role);
                }
                await userRoleStore.RemoveFromRoleAsync(user, normalizedRole, CancellationToken);
            }
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Gets a list of role names the specified <paramref name="user"/> belongs to.
        /// </summary>
        /// <param name="user">The user whose role names to retrieve.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing a list of role names.</returns>
        public virtual async Task<IList<string>> GetRolesAsync(TUser user)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await userRoleStore.GetRolesAsync(user, CancellationToken);
        }

        /// <summary>
        /// Returns a flag indicating whether the specified <paramref name="user"/> is a member of the given named role.
        /// </summary>
        /// <param name="user">The user whose role membership should be checked.</param>
        /// <param name="role">The name of the role to be checked.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing a flag indicating whether the specified <paramref name="user"/> is
        /// a member of the named role.
        /// </returns>
        public virtual async Task<bool> IsInRoleAsync(TUser user, string role)
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await userRoleStore.IsInRoleAsync(user, NormalizeName(role), CancellationToken);
        }

        /// <summary>
        /// Gets the email address for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose email should be returned.</param>
        /// <returns>The task object containing the results of the asynchronous operation, the email address for the specified <paramref name="user"/>.</returns>
        public virtual async Task<string> GetEmailAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await store.GetEmailAsync(user, CancellationToken);
        }

        /// <summary>
        /// Sets the <paramref name="email"/> address for a <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose email should be set.</param>
        /// <param name="email">The email to set.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> SetEmailAsync(TUser user, string email)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await store.SetEmailAsync(user, email, CancellationToken);
            await store.SetEmailConfirmedAsync(user, false, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Gets the user, if any, associated with the normalized value of the specified email address.
        /// Note: Its recommended that identityOptions.User.RequireUniqueEmail be set to true when using this method, otherwise
        /// the store may throw if there are users with duplicate emails.
        /// </summary>
        /// <param name="email">The email address to return the user for.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous lookup operation, the user, if any, associated with a normalized value of the specified email address.
        /// </returns>
        public virtual async Task<TUser> FindByEmailAsync(string email)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            email = NormalizeEmail(email);
            var user = await store.FindByEmailAsync(email, CancellationToken);

            // Need to potentially check all keys
            if (user == null && Options.Stores.ProtectPersonalData)
            {
                var keyRing = _services.GetService<ILookupProtectorKeyRing>();
                var protector = _services.GetService<ILookupProtector>();
                if (keyRing != null && protector != null)
                {
                    foreach (var key in keyRing.GetAllKeyIds())
                    {
                        var oldKey = protector.Protect(key, email);
                        user = await store.FindByEmailAsync(oldKey, CancellationToken);
                        if (user != null)
                        {
                            return user;
                        }
                    }
                }
            }
            return user;
        }

        /// <summary>
        /// Updates the normalized email for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose email address should be normalized and updated.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual async Task UpdateNormalizedEmailAsync(TUser user)
        {
            var store = GetEmailStore(throwOnFail: false);
            if (store != null)
            {
                var email = await GetEmailAsync(user);
                await store.SetNormalizedEmailAsync(user, ProtectPersonalData(NormalizeEmail(email)), CancellationToken);
            }
        }

        /// <summary>
        /// Generates an email confirmation token for the specified user.
        /// </summary>
        /// <param name="user">The user to generate an email confirmation token for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, an email confirmation token.
        /// </returns>
        public virtual Task<string> GenerateEmailConfirmationTokenAsync(TUser user)
        {
            ThrowIfDisposed();
            return GenerateUserTokenAsync(user, Options.Tokens.EmailConfirmationTokenProvider, ConfirmEmailTokenPurpose);
        }

        /// <summary>
        /// Validates that an email confirmation token matches the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to validate the token against.</param>
        /// <param name="token">The email confirmation token to validate.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> ConfirmEmailAsync(TUser user, string token)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!await VerifyUserTokenAsync(user, Options.Tokens.EmailConfirmationTokenProvider, ConfirmEmailTokenPurpose, token))
            {
                return IdentityResult.Failed(ErrorDescriber.InvalidToken());
            }
            await store.SetEmailConfirmedAsync(user, true, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Gets a flag indicating whether the email address for the specified <paramref name="user"/> has been verified, true if the email address is verified otherwise
        /// false.
        /// </summary>
        /// <param name="user">The user whose email confirmation status should be returned.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous operation, a flag indicating whether the email address for the specified <paramref name="user"/>
        /// has been confirmed or not.
        /// </returns>
        public virtual async Task<bool> IsEmailConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await store.GetEmailConfirmedAsync(user, CancellationToken);
        }

        /// <summary>
        /// Generates an email change token for the specified user.
        /// </summary>
        /// <param name="user">The user to generate an email change token for.</param>
        /// <param name="newEmail">The new email address.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, an email change token.
        /// </returns>
        public virtual Task<string> GenerateChangeEmailTokenAsync(TUser user, string newEmail)
        {
            ThrowIfDisposed();
            return GenerateUserTokenAsync(user, Options.Tokens.ChangeEmailTokenProvider, GetChangeEmailTokenPurpose(newEmail));
        }

        /// <summary>
        /// Updates a users emails if the specified email change <paramref name="token"/> is valid for the user.
        /// </summary>
        /// <param name="user">The user whose email should be updated.</param>
        /// <param name="newEmail">The new email address.</param>
        /// <param name="token">The change email token to be verified.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> ChangeEmailAsync(TUser user, string newEmail, string token)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Make sure the token is valid and the stamp matches
            if (!await VerifyUserTokenAsync(user, Options.Tokens.ChangeEmailTokenProvider, GetChangeEmailTokenPurpose(newEmail), token))
            {
                return IdentityResult.Failed(ErrorDescriber.InvalidToken());
            }
            var store = GetEmailStore();
            await store.SetEmailAsync(user, newEmail, CancellationToken);
            await store.SetEmailConfirmedAsync(user, true, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Gets the telephone number, if any, for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose telephone number should be retrieved.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user's telephone number, if any.</returns>
        public virtual async Task<string> GetPhoneNumberAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await store.GetPhoneNumberAsync(user, CancellationToken);
        }

        /// <summary>
        /// Sets the phone number for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose phone number to set.</param>
        /// <param name="phoneNumber">The phone number to set.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await store.SetPhoneNumberAsync(user, phoneNumber, CancellationToken);
            await store.SetPhoneNumberConfirmedAsync(user, false, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Sets the phone number for the specified <paramref name="user"/> if the specified
        /// change <paramref name="token"/> is valid.
        /// </summary>
        /// <param name="user">The user whose phone number to set.</param>
        /// <param name="phoneNumber">The phone number to set.</param>
        /// <param name="token">The phone number confirmation token to validate.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/>
        /// of the operation.
        /// </returns>
        public virtual async Task<IdentityResult> ChangePhoneNumberAsync(TUser user, string phoneNumber, string token)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!await VerifyChangePhoneNumberTokenAsync(user, token, phoneNumber))
            {
                Logger.LogWarning(7, "Change phone number for user {userId} failed with invalid token.", await GetUserIdAsync(user));
                return IdentityResult.Failed(ErrorDescriber.InvalidToken());
            }
            await store.SetPhoneNumberAsync(user, phoneNumber, CancellationToken);
            await store.SetPhoneNumberConfirmedAsync(user, true, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Gets a flag indicating whether the specified <paramref name="user"/>'s telephone number has been confirmed.
        /// </summary>
        /// <param name="user">The user to return a flag for, indicating whether their telephone number is confirmed.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, returning true if the specified <paramref name="user"/> has a confirmed
        /// telephone number otherwise false.
        /// </returns>
        public virtual Task<bool> IsPhoneNumberConfirmedAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return store.GetPhoneNumberConfirmedAsync(user, CancellationToken);
        }

        /// <summary>
        /// Generates a telephone number change token for the specified user.
        /// </summary>
        /// <param name="user">The user to generate a telephone number token for.</param>
        /// <param name="phoneNumber">The new phone number the validation token should be sent to.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the telephone change number token.
        /// </returns>
        public virtual Task<string> GenerateChangePhoneNumberTokenAsync(TUser user, string phoneNumber)
        {
            ThrowIfDisposed();
            return GenerateUserTokenAsync(user, Options.Tokens.ChangePhoneNumberTokenProvider, ChangePhoneNumberTokenPurpose + ":" + phoneNumber);
        }

        /// <summary>
        /// Returns a flag indicating whether the specified <paramref name="user"/>'s phone number change verification
        /// token is valid for the given <paramref name="phoneNumber"/>.
        /// </summary>
        /// <param name="user">The user to validate the token against.</param>
        /// <param name="token">The telephone number change token to validate.</param>
        /// <param name="phoneNumber">The telephone number the token was generated for.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, returning true if the <paramref name="token"/>
        /// is valid, otherwise false.
        /// </returns>
        public virtual Task<bool> VerifyChangePhoneNumberTokenAsync(TUser user, string token, string phoneNumber)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Make sure the token is valid and the stamp matches
            return VerifyUserTokenAsync(user, Options.Tokens.ChangePhoneNumberTokenProvider, ChangePhoneNumberTokenPurpose+":"+ phoneNumber, token);
        }

        /// <summary>
        /// Returns a flag indicating whether the specified <paramref name="token"/> is valid for
        /// the given <paramref name="user"/> and <paramref name="purpose"/>.
        /// </summary>
        /// <param name="user">The user to validate the token against.</param>
        /// <param name="tokenProvider">The token provider used to generate the token.</param>
        /// <param name="purpose">The purpose the token should be generated for.</param>
        /// <param name="token">The token to validate</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, returning true if the <paramref name="token"/>
        /// is valid, otherwise false.
        /// </returns>
        public virtual async Task<bool> VerifyUserTokenAsync(TUser user, string tokenProvider, string purpose, string token)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (tokenProvider == null)
            {
                throw new ArgumentNullException(nameof(tokenProvider));
            }

            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(Resources.FormatNoTokenProvider(nameof(TUser), tokenProvider));
            }
            // Make sure the token is valid
            var result = await _tokenProviders[tokenProvider].ValidateAsync(purpose, token, this, user);

            if (!result)
            {
                Logger.LogWarning(9, "VerifyUserTokenAsync() failed with purpose: {purpose} for user {userId}.", purpose, await GetUserIdAsync(user));
            }
            return result;
        }

        /// <summary>
        /// Generates a token for the given <paramref name="user"/> and <paramref name="purpose"/>.
        /// </summary>
        /// <param name="purpose">The purpose the token will be for.</param>
        /// <param name="user">The user the token will be for.</param>
        /// <param name="tokenProvider">The provider which will generate the token.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents result of the asynchronous operation, a token for
        /// the given user and purpose.
        /// </returns>
        public virtual Task<string> GenerateUserTokenAsync(TUser user, string tokenProvider, string purpose)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (tokenProvider == null)
            {
                throw new ArgumentNullException(nameof(tokenProvider));
            }
            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(Resources.FormatNoTokenProvider(nameof(TUser), tokenProvider));
            }

            return _tokenProviders[tokenProvider].GenerateAsync(purpose, this, user);
        }

        /// <summary>
        /// Registers a token provider.
        /// </summary>
        /// <param name="providerName">The name of the provider to register.</param>
        /// <param name="provider">The provider to register.</param>
        public virtual void RegisterTokenProvider(string providerName, IUserTwoFactorTokenProvider<TUser> provider)
        {
            ThrowIfDisposed();
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            _tokenProviders[providerName] = provider;
        }

        /// <summary>
        /// Gets a list of valid two factor token providers for the specified <paramref name="user"/>,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user the whose two factor authentication providers will be returned.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents result of the asynchronous operation, a list of two
        /// factor authentication providers for the specified user.
        /// </returns>
        public virtual async Task<IList<string>> GetValidTwoFactorProvidersAsync(TUser user)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
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
        /// Verifies the specified two factor authentication <paramref name="token" /> against the <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user the token is supposed to be for.</param>
        /// <param name="tokenProvider">The provider which will verify the token.</param>
        /// <param name="token">The token to verify.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents result of the asynchronous operation, true if the token is valid,
        /// otherwise false.
        /// </returns>
        public virtual async Task<bool> VerifyTwoFactorTokenAsync(TUser user, string tokenProvider, string token)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(Resources.FormatNoTokenProvider(nameof(TUser), tokenProvider));
            }

            // Make sure the token is valid
            var result = await _tokenProviders[tokenProvider].ValidateAsync("TwoFactor", token, this, user);
            if (!result)
            {
                Logger.LogWarning(10, $"{nameof(VerifyTwoFactorTokenAsync)}() failed for user {await GetUserIdAsync(user)}.");
            }
            return result;
        }

        /// <summary>
        /// Gets a two factor authentication token for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user the token is for.</param>
        /// <param name="tokenProvider">The provider which will generate the token.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents result of the asynchronous operation, a two factor authentication token
        /// for the user.
        /// </returns>
        public virtual Task<string> GenerateTwoFactorTokenAsync(TUser user, string tokenProvider)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (!_tokenProviders.ContainsKey(tokenProvider))
            {
                throw new NotSupportedException(Resources.FormatNoTokenProvider(nameof(TUser), tokenProvider));
            }

            return _tokenProviders[tokenProvider].GenerateAsync("TwoFactor", this, user);
        }

        /// <summary>
        /// Returns a flag indicating whether the specified <paramref name="user"/> has two factor authentication enabled or not,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose two factor authentication enabled status should be retrieved.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, true if the specified <paramref name="user "/>
        /// has two factor authentication enabled, otherwise false.
        /// </returns>
        public virtual async Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserTwoFactorStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await store.GetTwoFactorEnabledAsync(user, CancellationToken);
        }

        /// <summary>
        /// Sets a flag indicating whether the specified <paramref name="user"/> has two factor authentication enabled or not,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose two factor authentication enabled status should be set.</param>
        /// <param name="enabled">A flag indicating whether the specified <paramref name="user"/> has two factor authentication enabled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, the <see cref="IdentityResult"/> of the operation
        /// </returns>
        public virtual async Task<IdentityResult> SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();
            var store = GetUserTwoFactorStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await store.SetTwoFactorEnabledAsync(user, enabled, CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Returns a flag indicating whether the specified <paramref name="user"/> is locked out,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose locked out status should be retrieved.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, true if the specified <paramref name="user "/>
        /// is locked out, otherwise false.
        /// </returns>
        public virtual async Task<bool> IsLockedOutAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (!await store.GetLockoutEnabledAsync(user, CancellationToken))
            {
                return false;
            }
            var lockoutTime = await store.GetLockoutEndDateAsync(user, CancellationToken);
            return lockoutTime >= DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Sets a flag indicating whether the specified <paramref name="user"/> is locked out,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose locked out status should be set.</param>
        /// <param name="enabled">Flag indicating whether the user is locked out or not.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, the <see cref="IdentityResult"/> of the operation
        /// </returns>
        public virtual async Task<IdentityResult> SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await store.SetLockoutEnabledAsync(user, enabled, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Retrieves a flag indicating whether user lockout can be enabled for the specified user.
        /// </summary>
        /// <param name="user">The user whose ability to be locked out should be returned.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, true if a user can be locked out, otherwise false.
        /// </returns>
        public virtual async Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await store.GetLockoutEnabledAsync(user, CancellationToken);
        }

        /// <summary>
        /// Gets the last <see cref="DateTimeOffset"/> a user's last lockout expired, if any.
        /// A time value in the past indicates a user is not currently locked out.
        /// </summary>
        /// <param name="user">The user whose lockout date should be retrieved.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the lookup, a <see cref="DateTimeOffset"/> containing the last time a user's lockout expired, if any.
        /// </returns>
        public virtual async Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await store.GetLockoutEndDateAsync(user, CancellationToken);
        }

        /// <summary>
        /// Locks out a user until the specified end date has passed. Setting a end date in the past immediately unlocks a user.
        /// </summary>
        /// <param name="user">The user whose lockout date should be set.</param>
        /// <param name="lockoutEnd">The <see cref="DateTimeOffset"/> after which the <paramref name="user"/>'s lockout should end.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
        public virtual async Task<IdentityResult> SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!await store.GetLockoutEnabledAsync(user, CancellationToken))
            {
                Logger.LogWarning(11, "Lockout for user {userId} failed because lockout is not enabled for this user.", await GetUserIdAsync(user));
                return IdentityResult.Failed(ErrorDescriber.UserLockoutNotEnabled());
            }
            await store.SetLockoutEndDateAsync(user, lockoutEnd, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Increments the access failed count for the user as an asynchronous operation.
        /// If the failed access account is greater than or equal to the configured maximum number of attempts,
        /// the user will be locked out for the configured lockout time span.
        /// </summary>
        /// <param name="user">The user whose failed access count to increment.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
        public virtual async Task<IdentityResult> AccessFailedAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // If this puts the user over the threshold for lockout, lock them out and reset the access failed count
            var count = await store.IncrementAccessFailedCountAsync(user, CancellationToken);
            if (count < Options.Lockout.MaxFailedAccessAttempts)
            {
                return await UpdateUserAsync(user);
            }
            Logger.LogWarning(12, "User {userId} is locked out.", await GetUserIdAsync(user));
            await store.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.Add(Options.Lockout.DefaultLockoutTimeSpan),
                CancellationToken);
            await store.ResetAccessFailedCountAsync(user, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Resets the access failed count for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose failed access count should be reset.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the operation.</returns>
        public virtual async Task<IdentityResult> ResetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (await GetAccessFailedCountAsync(user) == 0)
            {
                return IdentityResult.Success;
            }
            await store.ResetAccessFailedCountAsync(user, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Retrieves the current number of failed accesses for the given <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose access failed count should be retrieved for.</param>
        /// <returns>The <see cref="Task"/> that contains the result the asynchronous operation, the current failed access count
        /// for the user.</returns>
        public virtual async Task<int> GetAccessFailedCountAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return await store.GetAccessFailedCountAsync(user, CancellationToken);
        }

        /// <summary>
        /// Returns a list of users from the user store who have the specified <paramref name="claim"/>.
        /// </summary>
        /// <param name="claim">The claim to look for.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a list of <typeparamref name="TUser"/>s who
        /// have the specified claim.
        /// </returns>
        public virtual Task<IList<TUser>> GetUsersForClaimAsync(Claim claim)
        {
            ThrowIfDisposed();
            var store = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            return store.GetUsersForClaimAsync(claim, CancellationToken);
        }

        /// <summary>
        /// Returns a list of users from the user store who are members of the specified <paramref name="roleName"/>.
        /// </summary>
        /// <param name="roleName">The name of the role whose users should be returned.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a list of <typeparamref name="TUser"/>s who
        /// are members of the specified role.
        /// </returns>
        public virtual Task<IList<TUser>> GetUsersInRoleAsync(string roleName)
        {
            ThrowIfDisposed();
            var store = GetUserRoleStore();
            if (roleName == null)
            {
                throw new ArgumentNullException(nameof(roleName));
            }

            return store.GetUsersInRoleAsync(NormalizeName(roleName), CancellationToken);
        }

        /// <summary>
        /// Returns an authentication token for a user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="loginProvider">The authentication scheme for the provider the token is associated with.</param>
        /// <param name="tokenName">The name of the token.</param>
        /// <returns>The authentication token for a user</returns>
        public virtual Task<string> GetAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName)
        {
            ThrowIfDisposed();
            var store = GetAuthenticationTokenStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }
            if (tokenName == null)
            {
                throw new ArgumentNullException(nameof(tokenName));
            }

            return store.GetTokenAsync(user, loginProvider, tokenName, CancellationToken);
        }

        /// <summary>
        /// Sets an authentication token for a user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="loginProvider">The authentication scheme for the provider the token is associated with.</param>
        /// <param name="tokenName">The name of the token.</param>
        /// <param name="tokenValue">The value of the token.</param>
        /// <returns>Whether the user was successfully updated.</returns>
        public virtual async Task<IdentityResult> SetAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName, string tokenValue)
        {
            ThrowIfDisposed();
            var store = GetAuthenticationTokenStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }
            if (tokenName == null)
            {
                throw new ArgumentNullException(nameof(tokenName));
            }

            // REVIEW: should updating any tokens affect the security stamp?
            await store.SetTokenAsync(user, loginProvider, tokenName, tokenValue, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Remove an authentication token for a user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="loginProvider">The authentication scheme for the provider the token is associated with.</param>
        /// <param name="tokenName">The name of the token.</param>
        /// <returns>Whether a token was removed.</returns>
        public virtual async Task<IdentityResult> RemoveAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName)
        {
            ThrowIfDisposed();
            var store = GetAuthenticationTokenStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }
            if (tokenName == null)
            {
                throw new ArgumentNullException(nameof(tokenName));
            }

            await store.RemoveTokenAsync(user, loginProvider, tokenName, CancellationToken);
            return await UpdateUserAsync(user);
        }

        /// <summary>
        /// Returns the authenticator key for the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The authenticator key</returns>
        public virtual Task<string> GetAuthenticatorKeyAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetAuthenticatorKeyStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return store.GetAuthenticatorKeyAsync(user, CancellationToken);
        }

        /// <summary>
        /// Resets the authenticator key for the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Whether the user was successfully updated.</returns>
        public virtual async Task<IdentityResult> ResetAuthenticatorKeyAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetAuthenticatorKeyStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            await store.SetAuthenticatorKeyAsync(user, GenerateNewAuthenticatorKey(), CancellationToken);
            await UpdateSecurityStampInternal(user);
            return await UpdateAsync(user);
        }

        /// <summary>
        /// Generates a new base32 encoded 160-bit security secret (size of SHA1 hash).
        /// </summary>
        /// <returns>The new security secret.</returns>
        public virtual string GenerateNewAuthenticatorKey()
            => NewSecurityStamp();

        /// <summary>
        /// Generates recovery codes for the user, this invalidates any previous recovery codes for the user.
        /// </summary>
        /// <param name="user">The user to generate recovery codes for.</param>
        /// <param name="number">The number of codes to generate.</param>
        /// <returns>The new recovery codes for the user.  Note: there may be less than number returned, as duplicates will be removed.</returns>
        public virtual async Task<IEnumerable<string>> GenerateNewTwoFactorRecoveryCodesAsync(TUser user, int number)
        {
            ThrowIfDisposed();
            var store = GetRecoveryCodeStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var newCodes = new List<string>(number);
            for (var i = 0; i < number; i++)
            {
                newCodes.Add(CreateTwoFactorRecoveryCode());
            }

            await store.ReplaceCodesAsync(user, newCodes.Distinct(), CancellationToken);
            var update = await UpdateAsync(user);
            if (update.Succeeded)
            {
                return newCodes;
            }
            return null;
        }

        /// <summary>
        /// Generate a new recovery code.
        /// </summary>
        /// <returns></returns>
        protected virtual string CreateTwoFactorRecoveryCode()
            => Guid.NewGuid().ToString().Substring(0, 8);

        /// <summary>
        /// Returns whether a recovery code is valid for a user. Note: recovery codes are only valid
        /// once, and will be invalid after use.
        /// </summary>
        /// <param name="user">The user who owns the recovery code.</param>
        /// <param name="code">The recovery code to use.</param>
        /// <returns>True if the recovery code was found for the user.</returns>
        public virtual async Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(TUser user, string code)
        {
            ThrowIfDisposed();
            var store = GetRecoveryCodeStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var success = await store.RedeemCodeAsync(user, code, CancellationToken);
            if (success)
            {
                return await UpdateAsync(user);
            }
            return IdentityResult.Failed(ErrorDescriber.RecoveryCodeRedemptionFailed());
        }

        /// <summary>
        /// Returns how many recovery code are still valid for a user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>How many recovery code are still valid for a user.</returns>
        public virtual Task<int> CountRecoveryCodesAsync(TUser user)
        {
            ThrowIfDisposed();
            var store = GetRecoveryCodeStore();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return store.CountCodesAsync(user, CancellationToken);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the role manager and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                Store.Dispose();
                _disposed = true;
            }
        }

        private IUserTwoFactorStore<TUser> GetUserTwoFactorStore()
        {
            var cast = Store as IUserTwoFactorStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserTwoFactorStore);
            }
            return cast;
        }

        private IUserLockoutStore<TUser> GetUserLockoutStore()
        {
            var cast = Store as IUserLockoutStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserLockoutStore);
            }
            return cast;
        }

        private IUserEmailStore<TUser> GetEmailStore(bool throwOnFail = true)
        {
            var cast = Store as IUserEmailStore<TUser>;
            if (throwOnFail && cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserEmailStore);
            }
            return cast;
        }

        private IUserPhoneNumberStore<TUser> GetPhoneNumberStore()
        {
            var cast = Store as IUserPhoneNumberStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserPhoneNumberStore);
            }
            return cast;
        }

        /// <summary>
        /// Creates bytes to use as a security token from the user's security stamp.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The security token bytes.</returns>
        public virtual async Task<byte[]> CreateSecurityTokenAsync(TUser user)
        {
            return Encoding.Unicode.GetBytes(await GetSecurityStampAsync(user));
        }

        // Update the security stamp if the store supports it
        private async Task UpdateSecurityStampInternal(TUser user)
        {
            if (SupportsUserSecurityStamp)
            {
                await GetSecurityStore().SetSecurityStampAsync(user, NewSecurityStamp(), CancellationToken);
            }
        }

        /// <summary>
        /// Updates a user's password hash.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="validatePassword">Whether to validate the password.</param>
        /// <returns>Whether the password has was successfully updated.</returns>
        protected virtual Task<IdentityResult> UpdatePasswordHash(TUser user, string newPassword, bool validatePassword)
            => UpdatePasswordHash(GetPasswordStore(), user, newPassword, validatePassword);

        private async Task<IdentityResult> UpdatePasswordHash(IUserPasswordStore<TUser> passwordStore,
            TUser user, string newPassword, bool validatePassword = true)
        {
            if (validatePassword)
            {
                var validate = await ValidatePasswordAsync(user, newPassword);
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

        private IUserRoleStore<TUser> GetUserRoleStore()
        {
            var cast = Store as IUserRoleStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserRoleStore);
            }
            return cast;
        }

        private static string NewSecurityStamp()
        {
            byte[] bytes = new byte[20];
#if NETSTANDARD2_0
            _rng.GetBytes(bytes);
#else
            RandomNumberGenerator.Fill(bytes);
#endif
            return Base32.ToBase32(bytes);
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

        private IUserSecurityStampStore<TUser> GetSecurityStore()
        {
            var cast = Store as IUserSecurityStampStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserSecurityStampStore);
            }
            return cast;
        }

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
        /// Generates the token purpose used to change email.
        /// </summary>
        /// <param name="newEmail">The new email address.</param>
        /// <returns>The token purpose.</returns>
        public static string GetChangeEmailTokenPurpose(string newEmail) => "ChangeEmail:" + newEmail;

        /// <summary>
        /// Should return <see cref="IdentityResult.Success"/> if validation is successful. This is
        /// called before saving the user via Create or Update.
        /// </summary>
        /// <param name="user">The user</param>
        /// <returns>A <see cref="IdentityResult"/> representing whether validation was successful.</returns>
        protected async Task<IdentityResult> ValidateUserAsync(TUser user)
        {
            if (SupportsUserSecurityStamp)
            {
                var stamp = await GetSecurityStampAsync(user);
                if (stamp == null)
                {
                    throw new InvalidOperationException(Resources.NullSecurityStamp);
                }
            }
            var errors = new List<IdentityError>();
            foreach (var v in UserValidators)
            {
                var result = await v.ValidateAsync(this, user);
                if (!result.Succeeded)
                {
                    errors.AddRange(result.Errors);
                }
            }
            if (errors.Count > 0)
            {
                Logger.LogWarning(13, "User {userId} validation failed: {errors}.", await GetUserIdAsync(user), string.Join(";", errors.Select(e => e.Code)));
                return IdentityResult.Failed(errors.ToArray());
            }
            return IdentityResult.Success;
        }

        /// <summary>
        /// Should return <see cref="IdentityResult.Success"/> if validation is successful. This is
        /// called before updating the password hash.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>A <see cref="IdentityResult"/> representing whether validation was successful.</returns>
        protected async Task<IdentityResult> ValidatePasswordAsync(TUser user, string password)
        {
            var errors = new List<IdentityError>();
            var isValid = true;
            foreach (var v in PasswordValidators)
            {
                var result = await v.ValidateAsync(this, user, password);
                if (!result.Succeeded)
                {
                    if (result.Errors.Any())
                    {
                        errors.AddRange(result.Errors);
                    }

                    isValid = false;
                }
            }
            if (!isValid)
            {
                Logger.LogWarning(14, "User {userId} password validation failed: {errors}.", await GetUserIdAsync(user), string.Join(";", errors.Select(e => e.Code)));
                return IdentityResult.Failed(errors.ToArray());
            }
            return IdentityResult.Success;
        }

        /// <summary>
        /// Called to update the user after validating and updating the normalized email/user name.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Whether the operation was successful.</returns>
        protected virtual async Task<IdentityResult> UpdateUserAsync(TUser user)
        {
            var result = await ValidateUserAsync(user);
            if (!result.Succeeded)
            {
                return result;
            }
            await UpdateNormalizedUserNameAsync(user);
            await UpdateNormalizedEmailAsync(user);
            return await Store.UpdateAsync(user, CancellationToken);
        }

        private IUserAuthenticatorKeyStore<TUser> GetAuthenticatorKeyStore()
        {
            var cast = Store as IUserAuthenticatorKeyStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserAuthenticatorKeyStore);
            }
            return cast;
        }

        private IUserTwoFactorRecoveryCodeStore<TUser> GetRecoveryCodeStore()
        {
            var cast = Store as IUserTwoFactorRecoveryCodeStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserTwoFactorRecoveryCodeStore);
            }
            return cast;
        }

        private IUserAuthenticationTokenStore<TUser> GetAuthenticationTokenStore()
        {
            var cast = Store as IUserAuthenticationTokenStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserAuthenticationTokenStore);
            }
            return cast;
        }

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
        /// Throws if this class has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

    }
}
