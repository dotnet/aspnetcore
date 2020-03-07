// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    public partial class IdentityRole : Microsoft.AspNetCore.Identity.IdentityRole<string>
    {
        public IdentityRole() { }
        public IdentityRole(string roleName) { }
    }
    public partial class IdentityRoleClaim<TKey> where TKey : System.IEquatable<TKey>
    {
        public IdentityRoleClaim() { }
        public virtual string ClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string ClaimValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual int Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual TKey RoleId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void InitializeFromClaim(System.Security.Claims.Claim other) { }
        public virtual System.Security.Claims.Claim ToClaim() { throw null; }
    }
    public partial class IdentityRole<TKey> where TKey : System.IEquatable<TKey>
    {
        public IdentityRole() { }
        public IdentityRole(string roleName) { }
        public virtual string ConcurrencyStamp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual TKey Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string NormalizedName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override string ToString() { throw null; }
    }
    public partial class IdentityUser : Microsoft.AspNetCore.Identity.IdentityUser<string>
    {
        public IdentityUser() { }
        public IdentityUser(string userName) { }
    }
    public partial class IdentityUserClaim<TKey> where TKey : System.IEquatable<TKey>
    {
        public IdentityUserClaim() { }
        public virtual string ClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string ClaimValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual int Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual TKey UserId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void InitializeFromClaim(System.Security.Claims.Claim claim) { }
        public virtual System.Security.Claims.Claim ToClaim() { throw null; }
    }
    public partial class IdentityUserLogin<TKey> where TKey : System.IEquatable<TKey>
    {
        public IdentityUserLogin() { }
        public virtual string LoginProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string ProviderDisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string ProviderKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual TKey UserId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class IdentityUserRole<TKey> where TKey : System.IEquatable<TKey>
    {
        public IdentityUserRole() { }
        public virtual TKey RoleId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual TKey UserId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class IdentityUserToken<TKey> where TKey : System.IEquatable<TKey>
    {
        public IdentityUserToken() { }
        public virtual string LoginProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual TKey UserId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.ProtectedPersonalDataAttribute]
        public virtual string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class IdentityUser<TKey> where TKey : System.IEquatable<TKey>
    {
        public IdentityUser() { }
        public IdentityUser(string userName) { }
        public virtual int AccessFailedCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string ConcurrencyStamp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.ProtectedPersonalDataAttribute]
        public virtual string Email { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.PersonalDataAttribute]
        public virtual bool EmailConfirmed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.PersonalDataAttribute]
        public virtual TKey Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual bool LockoutEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.DateTimeOffset? LockoutEnd { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string NormalizedEmail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string NormalizedUserName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string PasswordHash { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.ProtectedPersonalDataAttribute]
        public virtual string PhoneNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.PersonalDataAttribute]
        public virtual bool PhoneNumberConfirmed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual string SecurityStamp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.PersonalDataAttribute]
        public virtual bool TwoFactorEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [Microsoft.AspNetCore.Identity.ProtectedPersonalDataAttribute]
        public virtual string UserName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override string ToString() { throw null; }
    }
    public abstract partial class RoleStoreBase<TRole, TKey, TUserRole, TRoleClaim> : Microsoft.AspNetCore.Identity.IQueryableRoleStore<TRole>, Microsoft.AspNetCore.Identity.IRoleClaimStore<TRole>, Microsoft.AspNetCore.Identity.IRoleStore<TRole>, System.IDisposable where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TKey : System.IEquatable<TKey> where TUserRole : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>, new() where TRoleClaim : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>, new()
    {
        public RoleStoreBase(Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer) { }
        public Microsoft.AspNetCore.Identity.IdentityErrorDescriber ErrorDescriber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public abstract System.Linq.IQueryable<TRole> Roles { get; }
        public abstract System.Threading.Tasks.Task AddClaimAsync(TRole role, System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public virtual TKey ConvertIdFromString(string id) { throw null; }
        public virtual string ConvertIdToString(TKey id) { throw null; }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected virtual TRoleClaim CreateRoleClaim(TRole role, System.Security.Claims.Claim claim) { throw null; }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public void Dispose() { }
        public abstract System.Threading.Tasks.Task<TRole> FindByIdAsync(string id, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task<TRole> FindByNameAsync(string normalizedName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public virtual System.Threading.Tasks.Task<string> GetNormalizedRoleNameAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetRoleIdAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetRoleNameAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public abstract System.Threading.Tasks.Task RemoveClaimAsync(TRole role, System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public virtual System.Threading.Tasks.Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetRoleNameAsync(TRole role, string roleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected void ThrowIfDisposed() { }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public abstract partial class UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken> : Microsoft.AspNetCore.Identity.IQueryableUserStore<TUser>, Microsoft.AspNetCore.Identity.IUserAuthenticationTokenStore<TUser>, Microsoft.AspNetCore.Identity.IUserAuthenticatorKeyStore<TUser>, Microsoft.AspNetCore.Identity.IUserClaimStore<TUser>, Microsoft.AspNetCore.Identity.IUserEmailStore<TUser>, Microsoft.AspNetCore.Identity.IUserLockoutStore<TUser>, Microsoft.AspNetCore.Identity.IUserLoginStore<TUser>, Microsoft.AspNetCore.Identity.IUserPasswordStore<TUser>, Microsoft.AspNetCore.Identity.IUserPhoneNumberStore<TUser>, Microsoft.AspNetCore.Identity.IUserSecurityStampStore<TUser>, Microsoft.AspNetCore.Identity.IUserStore<TUser>, Microsoft.AspNetCore.Identity.IUserTwoFactorRecoveryCodeStore<TUser>, Microsoft.AspNetCore.Identity.IUserTwoFactorStore<TUser>, System.IDisposable where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TKey : System.IEquatable<TKey> where TUserClaim : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, new() where TUserLogin : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, new() where TUserToken : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>, new()
    {
        public UserStoreBase(Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer) { }
        public Microsoft.AspNetCore.Identity.IdentityErrorDescriber ErrorDescriber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public abstract System.Linq.IQueryable<TUser> Users { get; }
        public abstract System.Threading.Tasks.Task AddClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task AddLoginAsync(TUser user, Microsoft.AspNetCore.Identity.UserLoginInfo login, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected abstract System.Threading.Tasks.Task AddUserTokenAsync(TUserToken token);
        public virtual TKey ConvertIdFromString(string id) { throw null; }
        public virtual string ConvertIdToString(TKey id) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<int> CountCodesAsync(TUser user, System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected virtual TUserClaim CreateUserClaim(TUser user, System.Security.Claims.Claim claim) { throw null; }
        protected virtual TUserLogin CreateUserLogin(TUser user, Microsoft.AspNetCore.Identity.UserLoginInfo login) { throw null; }
        protected virtual TUserToken CreateUserToken(TUser user, string loginProvider, string name, string value) { throw null; }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public void Dispose() { }
        public abstract System.Threading.Tasks.Task<TUser> FindByEmailAsync(string normalizedEmail, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task<TUser> FindByIdAsync(string userId, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public abstract System.Threading.Tasks.Task<TUser> FindByNameAsync(string normalizedUserName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected abstract System.Threading.Tasks.Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, System.Threading.CancellationToken cancellationToken);
        protected abstract System.Threading.Tasks.Task<TUser> FindUserAsync(TKey userId, System.Threading.CancellationToken cancellationToken);
        protected abstract System.Threading.Tasks.Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken);
        protected abstract System.Threading.Tasks.Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken);
        public virtual System.Threading.Tasks.Task<int> GetAccessFailedCountAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetAuthenticatorKeyAsync(TUser user, System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public virtual System.Threading.Tasks.Task<string> GetEmailAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> GetEmailConfirmedAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> GetLockoutEnabledAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<System.DateTimeOffset?> GetLockoutEndDateAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public abstract System.Threading.Tasks.Task<System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.UserLoginInfo>> GetLoginsAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public virtual System.Threading.Tasks.Task<string> GetNormalizedEmailAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetNormalizedUserNameAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetPasswordHashAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetPhoneNumberAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> GetPhoneNumberConfirmedAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetSecurityStampAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GetTokenAsync(TUser user, string loginProvider, string name, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> GetTwoFactorEnabledAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetUserIdAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetUserNameAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public abstract System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersForClaimAsync(System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public virtual System.Threading.Tasks.Task<bool> HasPasswordAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task<int> IncrementAccessFailedCountAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> RedeemCodeAsync(TUser user, string code, System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract System.Threading.Tasks.Task RemoveClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task RemoveTokenAsync(TUser user, string loginProvider, string name, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected abstract System.Threading.Tasks.Task RemoveUserTokenAsync(TUserToken token);
        public abstract System.Threading.Tasks.Task ReplaceClaimAsync(TUser user, System.Security.Claims.Claim claim, System.Security.Claims.Claim newClaim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public virtual System.Threading.Tasks.Task ReplaceCodesAsync(TUser user, System.Collections.Generic.IEnumerable<string> recoveryCodes, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task ResetAccessFailedCountAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetAuthenticatorKeyAsync(TUser user, string key, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task SetEmailAsync(TUser user, string email, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetEmailConfirmedAsync(TUser user, bool confirmed, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetLockoutEnabledAsync(TUser user, bool enabled, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetLockoutEndDateAsync(TUser user, System.DateTimeOffset? lockoutEnd, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetNormalizedUserNameAsync(TUser user, string normalizedName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetPasswordHashAsync(TUser user, string passwordHash, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetPhoneNumberAsync(TUser user, string phoneNumber, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetSecurityStampAsync(TUser user, string stamp, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task SetTokenAsync(TUser user, string loginProvider, string name, string value, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task SetTwoFactorEnabledAsync(TUser user, bool enabled, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual System.Threading.Tasks.Task SetUserNameAsync(TUser user, string userName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected void ThrowIfDisposed() { }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public abstract partial class UserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> : Microsoft.AspNetCore.Identity.UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>, Microsoft.AspNetCore.Identity.IUserRoleStore<TUser>, Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : Microsoft.AspNetCore.Identity.IdentityUser<TKey> where TRole : Microsoft.AspNetCore.Identity.IdentityRole<TKey> where TKey : System.IEquatable<TKey> where TUserClaim : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey>, new() where TUserRole : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>, new() where TUserLogin : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>, new() where TUserToken : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>, new() where TRoleClaim : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>, new()
    {
        public UserStoreBase(Microsoft.AspNetCore.Identity.IdentityErrorDescriber describer) : base (default(Microsoft.AspNetCore.Identity.IdentityErrorDescriber)) { }
        public abstract System.Threading.Tasks.Task AddToRoleAsync(TUser user, string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected virtual TUserRole CreateUserRole(TUser user, TRole role) { throw null; }
        protected abstract System.Threading.Tasks.Task<TRole> FindRoleAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken);
        protected abstract System.Threading.Tasks.Task<TUserRole> FindUserRoleAsync(TKey userId, TKey roleId, System.Threading.CancellationToken cancellationToken);
        public abstract System.Threading.Tasks.Task<System.Collections.Generic.IList<string>> GetRolesAsync(TUser user, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        public abstract System.Threading.Tasks.Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
}
