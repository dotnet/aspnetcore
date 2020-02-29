// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity
{
    public partial class AuthenticatorTokenProvider<TUser> : Microsoft.AspNetCore.Identity.IUserTwoFactorTokenProvider<TUser> where TUser : class
    {
        public AuthenticatorTokenProvider() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> CanGenerateTwoFactorTokenAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GenerateAsync(string purpose, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> ValidateAsync(string purpose, string token, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
    }
    public partial class ClaimsIdentityOptions
    {
        public ClaimsIdentityOptions() { }
        public string EmailClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RoleClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string SecurityStampClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string UserIdClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string UserNameClaimType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DefaultPersonalDataProtector : Microsoft.AspNetCore.Identity.IPersonalDataProtector
    {
        public DefaultPersonalDataProtector(Microsoft.AspNetCore.Identity.ILookupProtectorKeyRing keyRing, Microsoft.AspNetCore.Identity.ILookupProtector protector) { }
        public virtual string Protect(string data) { throw null; }
        public virtual string Unprotect(string data) { throw null; }
    }
    public partial class DefaultUserConfirmation<TUser> : Microsoft.AspNetCore.Identity.IUserConfirmation<TUser> where TUser : class
    {
        public DefaultUserConfirmation() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> IsConfirmedAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
    }
    public partial class EmailTokenProvider<TUser> : Microsoft.AspNetCore.Identity.TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
    {
        public EmailTokenProvider() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<bool> CanGenerateTwoFactorTokenAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<string> GetUserModifierAsync(string purpose, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
    }
    public partial class IdentityBuilder
    {
        public IdentityBuilder(System.Type user, Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
        public IdentityBuilder(System.Type user, System.Type role, Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
        public System.Type RoleType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type UserType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddClaimsPrincipalFactory<TFactory>() where TFactory : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddErrorDescriber<TDescriber>() where TDescriber : Microsoft.AspNetCore.Identity.IdentityErrorDescriber { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddPasswordValidator<TValidator>() where TValidator : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddPersonalDataProtection<TProtector, TKeyRing>() where TProtector : class, Microsoft.AspNetCore.Identity.ILookupProtector where TKeyRing : class, Microsoft.AspNetCore.Identity.ILookupProtectorKeyRing { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddRoleManager<TRoleManager>() where TRoleManager : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddRoleStore<TStore>() where TStore : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddRoles<TRole>() where TRole : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddRoleValidator<TRole>() where TRole : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddTokenProvider(string providerName, System.Type provider) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddTokenProvider<TProvider>(string providerName) where TProvider : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddUserConfirmation<TUserConfirmation>() where TUserConfirmation : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddUserManager<TUserManager>() where TUserManager : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddUserStore<TStore>() where TStore : class { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityBuilder AddUserValidator<TValidator>() where TValidator : class { throw null; }
    }
    public partial class IdentityError
    {
        public IdentityError() { }
        public string Code { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Description { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class IdentityErrorDescriber
    {
        public IdentityErrorDescriber() { }
        public virtual Microsoft.AspNetCore.Identity.IdentityError ConcurrencyFailure() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError DefaultError() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError DuplicateEmail(string email) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError DuplicateRoleName(string role) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError DuplicateUserName(string userName) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError InvalidEmail(string email) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError InvalidRoleName(string role) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError InvalidToken() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError InvalidUserName(string userName) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError LoginAlreadyAssociated() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError PasswordMismatch() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError PasswordRequiresDigit() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError PasswordRequiresLower() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError PasswordRequiresNonAlphanumeric() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError PasswordRequiresUniqueChars(int uniqueChars) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError PasswordRequiresUpper() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError PasswordTooShort(int length) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError RecoveryCodeRedemptionFailed() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError UserAlreadyHasPassword() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError UserAlreadyInRole(string role) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError UserLockoutNotEnabled() { throw null; }
        public virtual Microsoft.AspNetCore.Identity.IdentityError UserNotInRole(string role) { throw null; }
    }
    public partial class IdentityOptions
    {
        public IdentityOptions() { }
        public Microsoft.AspNetCore.Identity.ClaimsIdentityOptions ClaimsIdentity { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.LockoutOptions Lockout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.PasswordOptions Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.SignInOptions SignIn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.StoreOptions Stores { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.TokenOptions Tokens { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.UserOptions User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class IdentityResult
    {
        public IdentityResult() { }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Identity.IdentityError> Errors { get { throw null; } }
        public bool Succeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public static Microsoft.AspNetCore.Identity.IdentityResult Success { get { throw null; } }
        public static Microsoft.AspNetCore.Identity.IdentityResult Failed(params Microsoft.AspNetCore.Identity.IdentityError[] errors) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial interface ILookupNormalizer
    {
        string NormalizeEmail(string email);
        string NormalizeName(string name);
    }
    public partial interface ILookupProtector
    {
        string Protect(string keyId, string data);
        string Unprotect(string keyId, string data);
    }
    public partial interface ILookupProtectorKeyRing
    {
        string CurrentKeyId { get; }
        string this[string keyId] { get; }
        System.Collections.Generic.IEnumerable<string> GetAllKeyIds();
    }
    public partial interface IPasswordHasher<TUser> where TUser : class
    {
        string HashPassword(TUser user, string password);
        Microsoft.AspNetCore.Identity.PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword);
    }
    public partial interface IPasswordValidator<TUser> where TUser : class
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user, string password);
    }
    public partial interface IPersonalDataProtector
    {
        string Protect(string data);
        string Unprotect(string data);
    }
    public partial interface IProtectedUserStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
    }
    public partial interface IQueryableRoleStore<TRole> : Microsoft.AspNetCore.Identity.IRoleStore<TRole>, System.IDisposable where TRole : class
    {
        System.Linq.IQueryable<TRole> Roles { get; }
    }
    public partial interface IQueryableUserStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Linq.IQueryable<TUser> Users { get; }
    }
    public partial interface IRoleClaimStore<TRole> : Microsoft.AspNetCore.Identity.IRoleStore<TRole>, System.IDisposable where TRole : class
    {
        System.Threading.Tasks.Task AddClaimAsync(TRole role, System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TRole role, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        System.Threading.Tasks.Task RemoveClaimAsync(TRole role, System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial interface IRoleStore<TRole> : System.IDisposable where TRole : class
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TRole role, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TRole role, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<TRole> FindByIdAsync(string roleId, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<TRole> FindByNameAsync(string normalizedRoleName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetNormalizedRoleNameAsync(TRole role, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetRoleIdAsync(TRole role, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetRoleNameAsync(TRole role, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetRoleNameAsync(TRole role, string roleName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TRole role, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IRoleValidator<TRole> where TRole : class
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateAsync(Microsoft.AspNetCore.Identity.RoleManager<TRole> manager, TRole role);
    }
    public partial interface IUserAuthenticationTokenStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<string> GetTokenAsync(TUser user, string loginProvider, string name, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task RemoveTokenAsync(TUser user, string loginProvider, string name, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetTokenAsync(TUser user, string loginProvider, string name, string value, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserAuthenticatorKeyStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<string> GetAuthenticatorKeyAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetAuthenticatorKeyAsync(TUser user, string key, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserClaimsPrincipalFactory<TUser> where TUser : class
    {
        System.Threading.Tasks.Task<System.Security.Claims.ClaimsPrincipal> CreateAsync(TUser user);
    }
    public partial interface IUserClaimStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task AddClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersForClaimAsync(System.Security.Claims.Claim claim, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task RemoveClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task ReplaceClaimAsync(TUser user, System.Security.Claims.Claim claim, System.Security.Claims.Claim newClaim, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserConfirmation<TUser> where TUser : class
    {
        System.Threading.Tasks.Task<bool> IsConfirmedAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user);
    }
    public partial interface IUserEmailStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<TUser> FindByEmailAsync(string normalizedEmail, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetEmailAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<bool> GetEmailConfirmedAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetNormalizedEmailAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetEmailAsync(TUser user, string email, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetEmailConfirmedAsync(TUser user, bool confirmed, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserLockoutStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<int> GetAccessFailedCountAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<bool> GetLockoutEnabledAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.DateTimeOffset?> GetLockoutEndDateAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<int> IncrementAccessFailedCountAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task ResetAccessFailedCountAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetLockoutEnabledAsync(TUser user, bool enabled, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetLockoutEndDateAsync(TUser user, System.DateTimeOffset? lockoutEnd, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserLoginStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task AddLoginAsync(TUser user, Microsoft.AspNetCore.Identity.UserLoginInfo login, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.UserLoginInfo>> GetLoginsAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserPasswordStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<string> GetPasswordHashAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<bool> HasPasswordAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetPasswordHashAsync(TUser user, string passwordHash, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserPhoneNumberStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<string> GetPhoneNumberAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<bool> GetPhoneNumberConfirmedAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetPhoneNumberAsync(TUser user, string phoneNumber, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserRoleStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task AddToRoleAsync(TUser user, string roleName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IList<string>> GetRolesAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersInRoleAsync(string roleName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<bool> IsInRoleAsync(TUser user, string roleName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task RemoveFromRoleAsync(TUser user, string roleName, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserSecurityStampStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<string> GetSecurityStampAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetSecurityStampAsync(TUser user, string stamp, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserStore<TUser> : System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<TUser> FindByIdAsync(string userId, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<TUser> FindByNameAsync(string normalizedUserName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetNormalizedUserNameAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetUserIdAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<string> GetUserNameAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetNormalizedUserNameAsync(TUser user, string normalizedName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetUserNameAsync(TUser user, string userName, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TUser user, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserTwoFactorRecoveryCodeStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<int> CountCodesAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task<bool> RedeemCodeAsync(TUser user, string code, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task ReplaceCodesAsync(TUser user, System.Collections.Generic.IEnumerable<string> recoveryCodes, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserTwoFactorStore<TUser> : Microsoft.AspNetCore.Identity.IUserStore<TUser>, System.IDisposable where TUser : class
    {
        System.Threading.Tasks.Task<bool> GetTwoFactorEnabledAsync(TUser user, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task SetTwoFactorEnabledAsync(TUser user, bool enabled, System.Threading.CancellationToken cancellationToken);
    }
    public partial interface IUserTwoFactorTokenProvider<TUser> where TUser : class
    {
        System.Threading.Tasks.Task<bool> CanGenerateTwoFactorTokenAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user);
        System.Threading.Tasks.Task<string> GenerateAsync(string purpose, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user);
        System.Threading.Tasks.Task<bool> ValidateAsync(string purpose, string token, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user);
    }
    public partial interface IUserValidator<TUser> where TUser : class
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user);
    }
    public partial class LockoutOptions
    {
        public LockoutOptions() { }
        public bool AllowedForNewUsers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan DefaultLockoutTimeSpan { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int MaxFailedAccessAttempts { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public enum PasswordHasherCompatibilityMode
    {
        IdentityV2 = 0,
        IdentityV3 = 1,
    }
    public partial class PasswordHasherOptions
    {
        public PasswordHasherOptions() { }
        public Microsoft.AspNetCore.Identity.PasswordHasherCompatibilityMode CompatibilityMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int IterationCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class PasswordHasher<TUser> : Microsoft.AspNetCore.Identity.IPasswordHasher<TUser> where TUser : class
    {
        public PasswordHasher(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.PasswordHasherOptions> optionsAccessor = null) { }
        public virtual string HashPassword(TUser user, string password) { throw null; }
        public virtual Microsoft.AspNetCore.Identity.PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword) { throw null; }
    }
    public partial class PasswordOptions
    {
        public PasswordOptions() { }
        public bool RequireDigit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int RequiredLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int RequiredUniqueChars { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireLowercase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireNonAlphanumeric { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireUppercase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class PasswordValidator<TUser> : Microsoft.AspNetCore.Identity.IPasswordValidator<TUser> where TUser : class
    {
        public PasswordValidator(Microsoft.AspNetCore.Identity.IdentityErrorDescriber errors = null) { }
        public Microsoft.AspNetCore.Identity.IdentityErrorDescriber Describer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual bool IsDigit(char c) { throw null; }
        public virtual bool IsLetterOrDigit(char c) { throw null; }
        public virtual bool IsLower(char c) { throw null; }
        public virtual bool IsUpper(char c) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user, string password) { throw null; }
    }
    public enum PasswordVerificationResult
    {
        Failed = 0,
        Success = 1,
        SuccessRehashNeeded = 2,
    }
    public partial class PersonalDataAttribute : System.Attribute
    {
        public PersonalDataAttribute() { }
    }
    public partial class PhoneNumberTokenProvider<TUser> : Microsoft.AspNetCore.Identity.TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
    {
        public PhoneNumberTokenProvider() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<bool> CanGenerateTwoFactorTokenAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<string> GetUserModifierAsync(string purpose, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
    }
    public partial class ProtectedPersonalDataAttribute : Microsoft.AspNetCore.Identity.PersonalDataAttribute
    {
        public ProtectedPersonalDataAttribute() { }
    }
    public partial class RoleManager<TRole> : System.IDisposable where TRole : class
    {
        public RoleManager(Microsoft.AspNetCore.Identity.IRoleStore<TRole> store, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Identity.IRoleValidator<TRole>> roleValidators, Microsoft.AspNetCore.Identity.ILookupNormalizer keyNormalizer, Microsoft.AspNetCore.Identity.IdentityErrorDescriber errors, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Identity.RoleManager<TRole>> logger) { }
        protected virtual System.Threading.CancellationToken CancellationToken { get { throw null; } }
        public Microsoft.AspNetCore.Identity.IdentityErrorDescriber ErrorDescriber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.ILookupNormalizer KeyNormalizer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual System.Linq.IQueryable<TRole> Roles { get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.IRoleValidator<TRole>> RoleValidators { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected Microsoft.AspNetCore.Identity.IRoleStore<TRole> Store { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual bool SupportsQueryableRoles { get { throw null; } }
        public virtual bool SupportsRoleClaims { get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AddClaimAsync(TRole role, System.Security.Claims.Claim claim) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TRole role) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TRole role) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Threading.Tasks.Task<TRole> FindByIdAsync(string roleId) { throw null; }
        public virtual System.Threading.Tasks.Task<TRole> FindByNameAsync(string roleName) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TRole role) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetRoleIdAsync(TRole role) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetRoleNameAsync(TRole role) { throw null; }
        public virtual string NormalizeKey(string key) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveClaimAsync(TRole role, System.Security.Claims.Claim claim) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> RoleExistsAsync(string roleName) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetRoleNameAsync(TRole role, string name) { throw null; }
        protected void ThrowIfDisposed() { }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TRole role) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task UpdateNormalizedRoleNameAsync(TRole role) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateRoleAsync(TRole role) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateRoleAsync(TRole role) { throw null; }
    }
    public partial class RoleValidator<TRole> : Microsoft.AspNetCore.Identity.IRoleValidator<TRole> where TRole : class
    {
        public RoleValidator(Microsoft.AspNetCore.Identity.IdentityErrorDescriber errors = null) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateAsync(Microsoft.AspNetCore.Identity.RoleManager<TRole> manager, TRole role) { throw null; }
    }
    public partial class SignInOptions
    {
        public SignInOptions() { }
        public bool RequireConfirmedAccount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireConfirmedEmail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireConfirmedPhoneNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class SignInResult
    {
        public SignInResult() { }
        public static Microsoft.AspNetCore.Identity.SignInResult Failed { get { throw null; } }
        public bool IsLockedOut { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public bool IsNotAllowed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public static Microsoft.AspNetCore.Identity.SignInResult LockedOut { get { throw null; } }
        public static Microsoft.AspNetCore.Identity.SignInResult NotAllowed { get { throw null; } }
        public bool RequiresTwoFactor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public bool Succeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] protected set { } }
        public static Microsoft.AspNetCore.Identity.SignInResult Success { get { throw null; } }
        public static Microsoft.AspNetCore.Identity.SignInResult TwoFactorRequired { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public partial class StoreOptions
    {
        public StoreOptions() { }
        public int MaxLengthForKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ProtectPersonalData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class TokenOptions
    {
        public static readonly string DefaultAuthenticatorProvider;
        public static readonly string DefaultEmailProvider;
        public static readonly string DefaultPhoneProvider;
        public static readonly string DefaultProvider;
        public TokenOptions() { }
        public string AuthenticatorIssuer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string AuthenticatorTokenProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ChangeEmailTokenProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ChangePhoneNumberTokenProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string EmailConfirmationTokenProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string PasswordResetTokenProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.Dictionary<string, Microsoft.AspNetCore.Identity.TokenProviderDescriptor> ProviderMap { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class TokenProviderDescriptor
    {
        public TokenProviderDescriptor(System.Type type) { }
        public object ProviderInstance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Type ProviderType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class TotpSecurityStampBasedTokenProvider<TUser> : Microsoft.AspNetCore.Identity.IUserTwoFactorTokenProvider<TUser> where TUser : class
    {
        protected TotpSecurityStampBasedTokenProvider() { }
        public abstract System.Threading.Tasks.Task<bool> CanGenerateTwoFactorTokenAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user);
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GenerateAsync(string purpose, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GetUserModifierAsync(string purpose, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> ValidateAsync(string purpose, string token, Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
    }
    public sealed partial class UpperInvariantLookupNormalizer : Microsoft.AspNetCore.Identity.ILookupNormalizer
    {
        public UpperInvariantLookupNormalizer() { }
        public string NormalizeEmail(string email) { throw null; }
        public string NormalizeName(string name) { throw null; }
    }
    public partial class UserClaimsPrincipalFactory<TUser> : Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<TUser> where TUser : class
    {
        public UserClaimsPrincipalFactory(Microsoft.AspNetCore.Identity.UserManager<TUser> userManager, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions> optionsAccessor) { }
        public Microsoft.AspNetCore.Identity.IdentityOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Identity.UserManager<TUser> UserManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Security.Claims.ClaimsPrincipal> CreateAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<System.Security.Claims.ClaimsIdentity> GenerateClaimsAsync(TUser user) { throw null; }
    }
    public partial class UserClaimsPrincipalFactory<TUser, TRole> : Microsoft.AspNetCore.Identity.UserClaimsPrincipalFactory<TUser> where TUser : class where TRole : class
    {
        public UserClaimsPrincipalFactory(Microsoft.AspNetCore.Identity.UserManager<TUser> userManager, Microsoft.AspNetCore.Identity.RoleManager<TRole> roleManager, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions> options) : base (default(Microsoft.AspNetCore.Identity.UserManager<TUser>), default(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions>)) { }
        public Microsoft.AspNetCore.Identity.RoleManager<TRole> RoleManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected override System.Threading.Tasks.Task<System.Security.Claims.ClaimsIdentity> GenerateClaimsAsync(TUser user) { throw null; }
    }
    public partial class UserLoginInfo
    {
        public UserLoginInfo(string loginProvider, string providerKey, string displayName) { }
        public string LoginProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ProviderDisplayName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ProviderKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class UserManager<TUser> : System.IDisposable where TUser : class
    {
        public const string ChangePhoneNumberTokenPurpose = "ChangePhoneNumber";
        public const string ConfirmEmailTokenPurpose = "EmailConfirmation";
        public const string ResetPasswordTokenPurpose = "ResetPassword";
        public UserManager(Microsoft.AspNetCore.Identity.IUserStore<TUser> store, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions> optionsAccessor, Microsoft.AspNetCore.Identity.IPasswordHasher<TUser> passwordHasher, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Identity.IUserValidator<TUser>> userValidators, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Identity.IPasswordValidator<TUser>> passwordValidators, Microsoft.AspNetCore.Identity.ILookupNormalizer keyNormalizer, Microsoft.AspNetCore.Identity.IdentityErrorDescriber errors, System.IServiceProvider services, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Identity.UserManager<TUser>> logger) { }
        protected virtual System.Threading.CancellationToken CancellationToken { get { throw null; } }
        public Microsoft.AspNetCore.Identity.IdentityErrorDescriber ErrorDescriber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.ILookupNormalizer KeyNormalizer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.IdentityOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Identity.IPasswordHasher<TUser> PasswordHasher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.IPasswordValidator<TUser>> PasswordValidators { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected internal Microsoft.AspNetCore.Identity.IUserStore<TUser> Store { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual bool SupportsQueryableUsers { get { throw null; } }
        public virtual bool SupportsUserAuthenticationTokens { get { throw null; } }
        public virtual bool SupportsUserAuthenticatorKey { get { throw null; } }
        public virtual bool SupportsUserClaim { get { throw null; } }
        public virtual bool SupportsUserEmail { get { throw null; } }
        public virtual bool SupportsUserLockout { get { throw null; } }
        public virtual bool SupportsUserLogin { get { throw null; } }
        public virtual bool SupportsUserPassword { get { throw null; } }
        public virtual bool SupportsUserPhoneNumber { get { throw null; } }
        public virtual bool SupportsUserRole { get { throw null; } }
        public virtual bool SupportsUserSecurityStamp { get { throw null; } }
        public virtual bool SupportsUserTwoFactor { get { throw null; } }
        public virtual bool SupportsUserTwoFactorRecoveryCodes { get { throw null; } }
        public virtual System.Linq.IQueryable<TUser> Users { get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.IUserValidator<TUser>> UserValidators { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AccessFailedAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AddClaimAsync(TUser user, System.Security.Claims.Claim claim) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AddClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AddLoginAsync(TUser user, Microsoft.AspNetCore.Identity.UserLoginInfo login) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AddPasswordAsync(TUser user, string password) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AddToRoleAsync(TUser user, string role) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> AddToRolesAsync(TUser user, System.Collections.Generic.IEnumerable<string> roles) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ChangeEmailAsync(TUser user, string newEmail, string token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ChangePasswordAsync(TUser user, string currentPassword, string newPassword) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ChangePhoneNumberAsync(TUser user, string phoneNumber, string token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> CheckPasswordAsync(TUser user, string password) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ConfirmEmailAsync(TUser user, string token) { throw null; }
        public virtual System.Threading.Tasks.Task<int> CountRecoveryCodesAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> CreateAsync(TUser user, string password) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<byte[]> CreateSecurityTokenAsync(TUser user) { throw null; }
        protected virtual string CreateTwoFactorRecoveryCode() { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> DeleteAsync(TUser user) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<TUser> FindByEmailAsync(string email) { throw null; }
        public virtual System.Threading.Tasks.Task<TUser> FindByIdAsync(string userId) { throw null; }
        public virtual System.Threading.Tasks.Task<TUser> FindByLoginAsync(string loginProvider, string providerKey) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<TUser> FindByNameAsync(string userName) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GenerateChangeEmailTokenAsync(TUser user, string newEmail) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GenerateChangePhoneNumberTokenAsync(TUser user, string phoneNumber) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GenerateConcurrencyStampAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GenerateEmailConfirmationTokenAsync(TUser user) { throw null; }
        public virtual string GenerateNewAuthenticatorKey() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<string>> GenerateNewTwoFactorRecoveryCodesAsync(TUser user, int number) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GeneratePasswordResetTokenAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GenerateTwoFactorTokenAsync(TUser user, string tokenProvider) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GenerateUserTokenAsync(TUser user, string tokenProvider, string purpose) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<int> GetAccessFailedCountAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName) { throw null; }
        public virtual System.Threading.Tasks.Task<string> GetAuthenticatorKeyAsync(TUser user) { throw null; }
        public static string GetChangeEmailTokenPurpose(string newEmail) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<System.Security.Claims.Claim>> GetClaimsAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GetEmailAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> GetLockoutEnabledAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.DateTimeOffset?> GetLockoutEndDateAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<Microsoft.AspNetCore.Identity.UserLoginInfo>> GetLoginsAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GetPhoneNumberAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<string>> GetRolesAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GetSecurityStampAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> GetTwoFactorEnabledAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<TUser> GetUserAsync(System.Security.Claims.ClaimsPrincipal principal) { throw null; }
        public virtual string GetUserId(System.Security.Claims.ClaimsPrincipal principal) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GetUserIdAsync(TUser user) { throw null; }
        public virtual string GetUserName(System.Security.Claims.ClaimsPrincipal principal) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<string> GetUserNameAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersForClaimAsync(System.Security.Claims.Claim claim) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<TUser>> GetUsersInRoleAsync(string roleName) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IList<string>> GetValidTwoFactorProvidersAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> HasPasswordAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> IsEmailConfirmedAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> IsInRoleAsync(TUser user, string role) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> IsLockedOutAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> IsPhoneNumberConfirmedAsync(TUser user) { throw null; }
        public virtual string NormalizeEmail(string email) { throw null; }
        public virtual string NormalizeName(string name) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RedeemTwoFactorRecoveryCodeAsync(TUser user, string code) { throw null; }
        public virtual void RegisterTokenProvider(string providerName, Microsoft.AspNetCore.Identity.IUserTwoFactorTokenProvider<TUser> provider) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName) { throw null; }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveClaimAsync(TUser user, System.Security.Claims.Claim claim) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveClaimsAsync(TUser user, System.Collections.Generic.IEnumerable<System.Security.Claims.Claim> claims) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveFromRoleAsync(TUser user, string role) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveFromRolesAsync(TUser user, System.Collections.Generic.IEnumerable<string> roles) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemoveLoginAsync(TUser user, string loginProvider, string providerKey) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> RemovePasswordAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ReplaceClaimAsync(TUser user, System.Security.Claims.Claim claim, System.Security.Claims.Claim newClaim) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ResetAccessFailedCountAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ResetAuthenticatorKeyAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ResetPasswordAsync(TUser user, string token, string newPassword) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName, string tokenValue) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetEmailAsync(TUser user, string email) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetLockoutEnabledAsync(TUser user, bool enabled) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetLockoutEndDateAsync(TUser user, System.DateTimeOffset? lockoutEnd) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetPhoneNumberAsync(TUser user, string phoneNumber) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetTwoFactorEnabledAsync(TUser user, bool enabled) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> SetUserNameAsync(TUser user, string userName) { throw null; }
        protected void ThrowIfDisposed() { }
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task UpdateNormalizedEmailAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task UpdateNormalizedUserNameAsync(TUser user) { throw null; }
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdatePasswordHash(TUser user, string newPassword, bool validatePassword) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateSecurityStampAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> UpdateUserAsync(TUser user) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidatePasswordAsync(TUser user, string password) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateUserAsync(TUser user) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> VerifyChangePhoneNumberTokenAsync(TUser user, string token, string phoneNumber) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.PasswordVerificationResult> VerifyPasswordAsync(Microsoft.AspNetCore.Identity.IUserPasswordStore<TUser> store, TUser user, string password) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> VerifyTwoFactorTokenAsync(TUser user, string tokenProvider, string token) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<bool> VerifyUserTokenAsync(TUser user, string tokenProvider, string purpose, string token) { throw null; }
    }
    public partial class UserOptions
    {
        public UserOptions() { }
        public string AllowedUserNameCharacters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RequireUniqueEmail { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class UserValidator<TUser> : Microsoft.AspNetCore.Identity.IUserValidator<TUser> where TUser : class
    {
        public UserValidator(Microsoft.AspNetCore.Identity.IdentityErrorDescriber errors = null) { }
        public Microsoft.AspNetCore.Identity.IdentityErrorDescriber Describer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Identity.IdentityResult> ValidateAsync(Microsoft.AspNetCore.Identity.UserManager<TUser> manager, TUser user) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class IdentityServiceCollectionExtensions
    {
        public static Microsoft.AspNetCore.Identity.IdentityBuilder AddIdentityCore<TUser>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TUser : class { throw null; }
        public static Microsoft.AspNetCore.Identity.IdentityBuilder AddIdentityCore<TUser>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.Identity.IdentityOptions> setupAction) where TUser : class { throw null; }
    }
}
namespace System.Security.Claims
{
    public static partial class PrincipalExtensions
    {
        public static string FindFirstValue(this System.Security.Claims.ClaimsPrincipal principal, string claimType) { throw null; }
    }
}
