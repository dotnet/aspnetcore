using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Exposes user related api which will automatically save changes to the UserStore
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class UserManager<TUser> : IDisposable where TUser : class
    {
        private readonly Dictionary<string, IUserTokenProvider<TUser>> _factors =
            new Dictionary<string, IUserTokenProvider<TUser>>();

        private IClaimsIdentityFactory<TUser> _claimsFactory;
        private TimeSpan _defaultLockout = TimeSpan.Zero;
        private bool _disposed;
        private IPasswordHasher _passwordHasher;

        public UserManager() { } 

        /// <summary>
        ///     Constructor which takes a service provider to find the default interfaces to hook up
        /// </summary>
        /// <param name="serviceProvider"></param>
        public UserManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }
            PasswordHasher = serviceProvider.GetService<IPasswordHasher>();
            UserValidator = serviceProvider.GetService<IUserValidator<TUser>>();
            PasswordValidator = serviceProvider.GetService<IPasswordValidator>();
            ClaimsIdentityFactory = serviceProvider.GetService<IClaimsIdentityFactory<TUser>>();
            Store = serviceProvider.GetService<IUserStore<TUser>>();
            // TODO: maybe each optional store as well?  Email and SMS services?
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="store">The IUserStore is responsible for commiting changes via the UpdateAsync/CreateAsync methods</param>
        public UserManager(IUserStore<TUser> store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            Store = store;
            UserValidator = new UserValidator<TUser>();
            PasswordHasher = new PasswordHasher();
            ClaimsIdentityFactory = new ClaimsIdentityFactory<TUser>();
        }

        /// <summary>
        ///     Persistence abstraction that the Manager operates against
        /// </summary>
        protected internal IUserStore<TUser> Store { get; set; }

        /// <summary>
        ///     Used to hash/verify passwords
        /// </summary>
        public IPasswordHasher PasswordHasher
        {
            get
            {
                ThrowIfDisposed();
                return _passwordHasher;
            }
            set
            {
                ThrowIfDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _passwordHasher = value;
            }
        }

        /// <summary>
        ///     Used to validate users before persisting changes
        /// </summary>
        public IUserValidator<TUser> UserValidator { get; set; }

        /// <summary>
        ///     Used to validate passwords before persisting changes
        /// </summary>
        public IPasswordValidator PasswordValidator { get; set; }

        /// <summary>
        ///     Used to create claims identities from users
        /// </summary>
        public IClaimsIdentityFactory<TUser> ClaimsIdentityFactory
        {
            get
            {
                ThrowIfDisposed();
                return _claimsFactory;
            }
            set
            {
                ThrowIfDisposed();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _claimsFactory = value;
            }
        }

        /// <summary>
        ///     Used to send email
        /// </summary>
        public IIdentityMessageService EmailService { get; set; }

        /// <summary>
        ///     Used to send a sms message
        /// </summary>
        public IIdentityMessageService SmsService { get; set; }

        /// <summary>
        ///     Used for generating ResetPassword and Confirmation Tokens
        /// </summary>
        public IUserTokenProvider<TUser> UserTokenProvider { get; set; }

        /// <summary>
        ///     If true, will enable user lockout when users are created
        /// </summary>
        public bool UserLockoutEnabledByDefault { get; set; }

        /// <summary>
        ///     Number of access attempts allowed for a user before lockout (if enabled)
        /// </summary>
        public int MaxFailedAccessAttemptsBeforeLockout { get; set; }

        /// <summary>
        ///     Default amount of time an user is locked out for after MaxFailedAccessAttempsBeforeLockout is reached
        /// </summary>
        public TimeSpan DefaultAccountLockoutTimeSpan
        {
            get { return _defaultLockout; }
            set { _defaultLockout = value; }
        }

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

        /// <summary>
        ///     Dictionary mapping user two factor providers
        /// </summary>
        public IDictionary<string, IUserTokenProvider<TUser>> TwoFactorProviders
        {
            get { return _factors; }
        }

        /// <summary>
        ///     Dispose the store context
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates a ClaimsIdentity representing the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authenticationType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<ClaimsIdentity> CreateIdentity(TUser user, string authenticationType, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return ClaimsIdentityFactory.Create(this, user, authenticationType, cancellationToken);
        }

        private async Task<IdentityResult> ValidateUserInternal(TUser user, CancellationToken cancellationToken)
        {
            return (UserValidator == null)
                ? IdentityResult.Success
                : await UserValidator.Validate(this, user, cancellationToken);
        }

        /// <summary>
        ///     Create a user with no password
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> Create(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            await UpdateSecurityStampInternal(user, cancellationToken);
            var result = await ValidateUserInternal(user, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            if (UserLockoutEnabledByDefault && SupportsUserLockout)
            {
                await GetUserLockoutStore().SetLockoutEnabled(user, true, cancellationToken);
            }
            await Store.Create(user, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        ///     Update a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> Update(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            var result = await ValidateUserInternal(user, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            await Store.Update(user, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        ///     Delete a user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> Delete(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            await Store.Delete(user, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        ///     Find a user by id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindById(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return Store.FindById(userId, cancellationToken);
        }

        /// <summary>
        ///     Find a user by name
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindByName(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            return Store.FindByName(userName, cancellationToken);
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
        ///     Create a user and associates it with the given password (if one is provided)
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> Create(TUser user, string password, CancellationToken cancellationToken = default(CancellationToken))
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
            var result = await UpdatePasswordInternal(passwordStore, user, password, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            return await Create(user, cancellationToken);
        }

        /// <summary>
        /// Get the user's name
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetUserName(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.GetUserName(user, cancellationToken);
        }

        /// <summary>
        /// Get the user's id
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetUserId(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.GetUserId(user, cancellationToken);
        }

        /// <summary>
        ///     Return a user with the specified username and password or null if there is no match.
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<TUser> Find(string userName, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var user = await FindByName(userName, cancellationToken);
            if (user == null)
            {
                return null;
            }
            return await CheckPassword(user, password, cancellationToken) ? user : null;
        }

        /// <summary>
        ///     Returns true if the password combination is valid for the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> CheckPassword(TUser user, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            if (user == null)
            {
                return false;
            }
            return await VerifyPassword(passwordStore, user, password, cancellationToken);
        }

        /// <summary>
        ///     Returns true if the user has a password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> HasPassword(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await passwordStore.HasPassword(user, cancellationToken);
        }

        /// <summary>
        ///     Add a user password only if one does not already exist
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddPassword(string userId, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            var hash = await passwordStore.GetPasswordHash(user, cancellationToken);
            if (hash != null)
            {
                return new IdentityResult(Resources.UserAlreadyHasPassword);
            }
            var result = await UpdatePasswordInternal(passwordStore, user, password, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Change a user password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="currentPassword"></param>
        /// <param name="newPassword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ChangePassword(string userId, string currentPassword,
            string newPassword, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (await VerifyPassword(passwordStore, user, currentPassword, cancellationToken))
            {
                var result = await UpdatePasswordInternal(passwordStore, user, newPassword, cancellationToken);
                if (!result.Succeeded)
                {
                    return result;
                }
                return await Update(user, cancellationToken);
            }
            return IdentityResult.Failed(Resources.PasswordMismatch);
        }

        /// <summary>
        ///     Remove a user's password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemovePassword(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var passwordStore = GetPasswordStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await passwordStore.SetPasswordHash(user, null, cancellationToken);
            await UpdateSecurityStampInternal(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        internal async Task<IdentityResult> UpdatePasswordInternal(IUserPasswordStore<TUser> passwordStore,
            TUser user, string newPassword, CancellationToken cancellationToken)
        {
            if (PasswordValidator != null)
            {
                var result = await PasswordValidator.Validate(newPassword, cancellationToken);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            await
                passwordStore.SetPasswordHash(user, PasswordHasher.HashPassword(newPassword), cancellationToken);
            await UpdateSecurityStampInternal(user, cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>
        ///     By default, retrieves the hashed password from the user store and calls PasswordHasher.VerifyHashPassword
        /// </summary>
        /// <param name="store"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<bool> VerifyPassword(IUserPasswordStore<TUser> store, TUser user,
            string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            var hash = await store.GetPasswordHash(user, cancellationToken);
            return PasswordHasher.VerifyHashedPassword(hash, password) != PasswordVerificationResult.Failed;
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
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetSecurityStamp(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var securityStore = GetSecurityStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await securityStore.GetSecurityStamp(user, cancellationToken);
        }

        /// <summary>
        ///     Generate a new security stamp for a user, used for SignOutEverywhere functionality
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> UpdateSecurityStamp(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var securityStore = GetSecurityStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await UpdateSecurityStampInternal(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Generate a password reset token for the user using the UserTokenProvider
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GeneratePasswordResetToken(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await GenerateUserToken("ResetPassword", userId, cancellationToken);
        }

        /// <summary>
        ///     Reset a user's password using a reset password token
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="token"></param>
        /// <param name="newPassword"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ResetPassword(string userId, string token, string newPassword, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            // Make sure the token is valid and the stamp matches
            if (!await VerifyUserToken(userId, "ResetPassword", token, cancellationToken))
            {
                return IdentityResult.Failed(Resources.InvalidToken);
            }
            var passwordStore = GetPasswordStore();
            var result = await UpdatePasswordInternal(passwordStore, user, newPassword, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            return await Update(user, cancellationToken);
        }

        // Update the security stamp if the store supports it
        internal async Task UpdateSecurityStampInternal(TUser user, CancellationToken cancellationToken)
        {
            if (SupportsUserSecurityStamp)
            {
                await GetSecurityStore().SetSecurityStamp(user, NewSecurityStamp(), cancellationToken);
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
        ///     Returns the user associated with this login
        /// </summary>
        /// <returns></returns>
        public virtual Task<TUser> Find(UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return GetLoginStore().Find(login, cancellationToken);
        }

        /// <summary>
        ///     Remove a user login
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="login"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveLogin(string userId, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await loginStore.RemoveLogin(user, login, cancellationToken);
            await UpdateSecurityStampInternal(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Associate a login with a user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="login"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddLogin(string userId, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            if (login == null)
            {
                throw new ArgumentNullException("login");
            }
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            var existingUser = await Find(login, cancellationToken);
            if (existingUser != null)
            {
                return IdentityResult.Failed(Resources.ExternalLoginExists);
            }
            await loginStore.AddLogin(user, login, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Gets the logins for a user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IList<UserLoginInfo>> GetLogins(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var loginStore = GetLoginStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await loginStore.GetLogins(user, cancellationToken);
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
        /// <param name="userId"></param>
        /// <param name="claim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddClaim(string userId, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await claimStore.AddClaim(user, claim, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Remove a user claim
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="claim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveClaim(string userId, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await claimStore.RemoveClaim(user, claim, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Get a users's claims
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IList<Claim>> GetClaims(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await claimStore.GetClaims(user, cancellationToken);
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
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddToRole(string userId, string role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            var userRoles = await userRoleStore.GetRoles(user, cancellationToken);
            if (userRoles.Contains(role))
            {
                return new IdentityResult(Resources.UserAlreadyInRole);
            }
            await userRoleStore.AddToRole(user, role, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Remove a user from a role.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveFromRole(string userId, string role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!await userRoleStore.IsInRole(user, role, cancellationToken))
            {
                return new IdentityResult(Resources.UserNotInRole);
            }
            await userRoleStore.RemoveFromRole(user, role, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Returns the roles for the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IList<string>> GetRoles(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await userRoleStore.GetRoles(user, cancellationToken);
        }

        /// <summary>
        ///     Returns true if the user is in the specified role
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsInRole(string userId, string role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var userRoleStore = GetUserRoleStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await userRoleStore.IsInRole(user, role);
        }

        // IUserEmailStore methods
        internal IUserEmailStore<TUser> GetEmailStore()
        {
            var cast = Store as IUserEmailStore<TUser>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIUserEmailStore);
            }
            return cast;
        }

        /// <summary>
        ///     Get a user's email
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<string> GetEmail(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetEmail(user, cancellationToken);
        }

        /// <summary>
        ///     Set a user's email
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetEmail(string userId, string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await store.SetEmail(user, email, cancellationToken);
            await store.SetEmailConfirmed(user, false, cancellationToken);
            await UpdateSecurityStampInternal(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Find a user by his email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual Task<TUser> FindByEmail(string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            if (email == null)
            {
                throw new ArgumentNullException("email");
            }
            return store.FindByEmail(email, cancellationToken);
        }

        /// <summary>
        ///     Get the confirmation token for the user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual Task<string> GenerateEmailConfirmationToken(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return GenerateUserToken("Confirmation", userId, cancellationToken);
        }

        /// <summary>
        ///     Confirm the user with confirmation token
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ConfirmEmail(string userId, string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!await VerifyUserToken(userId, "Confirmation", token, cancellationToken))
            {
                return IdentityResult.Failed(Resources.InvalidToken);
            }
            await store.SetEmailConfirmed(user, true, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Returns true if the user's email has been confirmed
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsEmailConfirmed(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetEmailStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetEmailConfirmed(user, cancellationToken);
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
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetPhoneNumber(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetPhoneNumber(user, cancellationToken);
        }

        /// <summary>
        ///     Set a user's phoneNumber
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetPhoneNumber(string userId, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await store.SetPhoneNumber(user, phoneNumber, cancellationToken);
            await store.SetPhoneNumberConfirmed(user, false, cancellationToken);
            await UpdateSecurityStampInternal(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Set a user's phoneNumber with the verification token
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ChangePhoneNumber(string userId, string phoneNumber, string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!await VerifyChangePhoneNumberToken(userId, token, phoneNumber))
            {
                return IdentityResult.Failed(Resources.InvalidToken);
            }
            await store.SetPhoneNumber(user, phoneNumber, cancellationToken);
            await store.SetPhoneNumberConfirmed(user, true, cancellationToken);
            await UpdateSecurityStampInternal(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Returns true if the user's phone number has been confirmed
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsPhoneNumberConfirmed(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetPhoneNumberStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetPhoneNumberConfirmed(user, cancellationToken);
        }

        // Two factor APIS

#if NET45
        internal async Task<SecurityToken> CreateSecurityToken(string userId)
        {
            return
                new SecurityToken(Encoding.Unicode.GetBytes(await GetSecurityStamp(userId)));
        }

        /// <summary>
        ///     Get a phone number code for a user and phone number
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateChangePhoneNumberToken(string userId, string phoneNumber)
        {
            ThrowIfDisposed();
            return
                Rfc6238AuthenticationService.GenerateCode(await CreateSecurityToken(userId), phoneNumber)
                    .ToString(CultureInfo.InvariantCulture);
        }
#endif

        /// <summary>
        ///     Verify a phone number code for a specific user and phone number
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="token"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public virtual async Task<bool> VerifyChangePhoneNumberToken(string userId, string token, string phoneNumber)
        {
            ThrowIfDisposed();
#if NET45
            var securityToken = await CreateSecurityToken(userId);
            int code;
            if (securityToken != null && Int32.TryParse(token, out code))
            {
                return Rfc6238AuthenticationService.ValidateCode(securityToken, code, phoneNumber);
            }
#endif
            return false;
        }

        /// <summary>
        ///     Verify a user token with the specified purpose
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="purpose"></param>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> VerifyUserToken(string userId, string purpose, string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (UserTokenProvider == null)
            {
                throw new NotSupportedException(Resources.NoTokenProvider);
            }
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            // Make sure the token is valid
            return await UserTokenProvider.Validate(purpose, token, this, user, cancellationToken);
        }

        /// <summary>
        ///     Get a user token for a specific purpose
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateUserToken(string purpose, string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (UserTokenProvider == null)
            {
                throw new NotSupportedException(Resources.NoTokenProvider);
            }
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await UserTokenProvider.Generate(purpose, this, user, cancellationToken);
        }

        /// <summary>
        ///     Register a user two factor provider
        /// </summary>
        /// <param name="twoFactorProvider"></param>
        /// <param name="provider"></param>
        public virtual void RegisterTwoFactorProvider(string twoFactorProvider, IUserTokenProvider<TUser> provider)
        {
            ThrowIfDisposed();
            if (twoFactorProvider == null)
            {
                throw new ArgumentNullException("twoFactorProvider");
            }
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }
            TwoFactorProviders[twoFactorProvider] = provider;
        }

        /// <summary>
        ///     Returns a list of valid two factor providers for a user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IList<string>> GetValidTwoFactorProviders(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            var results = new List<string>();
            foreach (var f in TwoFactorProviders)
            {
                if (await f.Value.IsValidProviderForUser(this, user, cancellationToken))
                {
                    results.Add(f.Key);
                }
            }
            return results;
        }

        /// <summary>
        ///     Verify a user token with the specified provider
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="twoFactorProvider"></param>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> VerifyTwoFactorToken(string userId, string twoFactorProvider, string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!_factors.ContainsKey(twoFactorProvider))
            {
                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, Resources.NoTwoFactorProvider,
                    twoFactorProvider));
            }
            // Make sure the token is valid
            var provider = _factors[twoFactorProvider];
            return await provider.Validate(twoFactorProvider, token, this, user, cancellationToken);
        }

        /// <summary>
        ///     Get a user token for a specific user factor provider
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="twoFactorProvider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GenerateTwoFactorToken(string userId, string twoFactorProvider, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!_factors.ContainsKey(twoFactorProvider))
            {
                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, Resources.NoTwoFactorProvider,
                    twoFactorProvider));
            }
            return await _factors[twoFactorProvider].Generate(twoFactorProvider, this, user, cancellationToken);
        }

        /// <summary>
        ///     Notify a user with a token from a specific user factor provider
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="twoFactorProvider"></param>
        /// <param name="token"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> NotifyTwoFactorToken(string userId, string twoFactorProvider,
            string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!_factors.ContainsKey(twoFactorProvider))
            {
                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, Resources.NoTwoFactorProvider,
                    twoFactorProvider));
            }
            await _factors[twoFactorProvider].Notify(token, this, user, cancellationToken);
            return IdentityResult.Success;
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
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> GetTwoFactorEnabled(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserTwoFactorStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetTwoFactorEnabled(user, cancellationToken);
        }

        /// <summary>
        ///     Set whether a user has two factor enabled or not
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="enabled"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetTwoFactorEnabled(string userId, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserTwoFactorStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await store.SetTwoFactorEnabled(user, enabled, cancellationToken);
            await UpdateSecurityStampInternal(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        // SMS/Email methods

        /// <summary>
        ///     Send an email to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task SendEmail(string userId, string subject, string body, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (EmailService != null)
            {
                var msg = new IdentityMessage
                {
                    Destination = await GetEmail(userId, cancellationToken),
                    Subject = subject,
                    Body = body,
                };
                await EmailService.Send(msg, cancellationToken);
            }
        }

        /// <summary>
        ///     Send a user a sms message
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task SendSms(string userId, string message, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (SmsService != null)
            {
                var msg = new IdentityMessage
                {
                    Destination = await GetPhoneNumber(userId, cancellationToken),
                    Body = message
                };
                await SmsService.Send(msg, cancellationToken);
            }
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
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> IsLockedOut(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!await store.GetLockoutEnabled(user, cancellationToken))
            {
                return false;
            }
            var lockoutTime = await store.GetLockoutEndDate(user, cancellationToken).ConfigureAwait((false));
            return lockoutTime >= DateTimeOffset.UtcNow;
        }

        /// <summary>
        ///     Sets whether the user allows lockout
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetLockoutEnabled(string userId, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await store.SetLockoutEnabled(user, enabled, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Returns whether the user allows lockout
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<bool> GetLockoutEnabled(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetLockoutEnabled(user, cancellationToken);
        }

        /// <summary>
        ///     Returns the user lockout end date
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<DateTimeOffset> GetLockoutEndDate(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetLockoutEndDate(user);
        }

        /// <summary>
        ///     Sets the user lockout end date
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="lockoutEnd"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetLockoutEndDate(string userId, DateTimeOffset lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            if (!await store.GetLockoutEnabled(user, cancellationToken).ConfigureAwait((false)))
            {
                return IdentityResult.Failed(Resources.LockoutNotEnabled);
            }
            await store.SetLockoutEndDate(user, lockoutEnd, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Increments the access failed count for the user and if the failed access account is greater than or equal
        ///     to the MaxFailedAccessAttempsBeforeLockout, the user will be locked out for the next DefaultAccountLockoutTimeSpan
        ///     and the AccessFailedCount will be reset to 0.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AccessFailed(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            // If this puts the user over the threshold for lockout, lock them out and reset the access failed count
            var count = await store.IncrementAccessFailedCount(user, cancellationToken);
            if (count < MaxFailedAccessAttemptsBeforeLockout)
            {
                return await Update(user, cancellationToken);
            }
            await
                store.SetLockoutEndDate(user, DateTimeOffset.UtcNow.Add(DefaultAccountLockoutTimeSpan), cancellationToken);
            await store.ResetAccessFailedCount(user, cancellationToken);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Resets the access failed count for the user to 0
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> ResetAccessFailedCount(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            await store.ResetAccessFailedCount(user);
            return await Update(user, cancellationToken);
        }

        /// <summary>
        ///     Returns the number of failed access attempts for the user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public virtual async Task<int> GetAccessFailedCount(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var store = GetUserLockoutStore();
            var user = await FindById(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.UserIdNotFound,
                    userId));
            }
            return await store.GetAccessFailedCount(user, cancellationToken);
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