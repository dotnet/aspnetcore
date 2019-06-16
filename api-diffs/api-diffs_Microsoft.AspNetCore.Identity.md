# Microsoft.AspNetCore.Identity

``` diff
 namespace Microsoft.AspNetCore.Identity {
     public class AspNetRoleManager<TRole> : RoleManager<TRole>, IDisposable where TRole : class {
         public AspNetRoleManager(IRoleStore<TRole> store, IEnumerable<IRoleValidator<TRole>> roleValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, ILogger<RoleManager<TRole>> logger, IHttpContextAccessor contextAccessor);
         protected override CancellationToken CancellationToken { get; }
     }
     public class AspNetUserManager<TUser> : UserManager<TUser>, IDisposable where TUser : class {
         public AspNetUserManager(IUserStore<TUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<TUser> passwordHasher, IEnumerable<IUserValidator<TUser>> userValidators, IEnumerable<IPasswordValidator<TUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<TUser>> logger);
         protected override CancellationToken CancellationToken { get; }
     }
     public class AuthenticatorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : class {
         public AuthenticatorTokenProvider();
         public virtual Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
         public virtual Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user);
         public virtual Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user);
     }
     public class ClaimsIdentityOptions {
         public ClaimsIdentityOptions();
         public string RoleClaimType { get; set; }
         public string SecurityStampClaimType { get; set; }
         public string UserIdClaimType { get; set; }
         public string UserNameClaimType { get; set; }
     }
     public class DataProtectionTokenProviderOptions {
         public DataProtectionTokenProviderOptions();
         public string Name { get; set; }
         public TimeSpan TokenLifespan { get; set; }
     }
     public class DataProtectorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : class {
         public DataProtectorTokenProvider(IDataProtectionProvider dataProtectionProvider, IOptions<DataProtectionTokenProviderOptions> options);
         public string Name { get; }
         protected DataProtectionTokenProviderOptions Options { get; private set; }
         protected IDataProtector Protector { get; private set; }
         public virtual Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
         public virtual Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user);
         public virtual Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user);
     }
     public class DefaultPersonalDataProtector : IPersonalDataProtector {
         public DefaultPersonalDataProtector(ILookupProtectorKeyRing keyRing, ILookupProtector protector);
         public virtual string Protect(string data);
         public virtual string Unprotect(string data);
     }
+    public class DefaultUserConfirmation<TUser> : IUserConfirmation<TUser> where TUser : class {
+        public DefaultUserConfirmation();
+        public virtual Task<bool> IsConfirmedAsync(UserManager<TUser> manager, TUser user);
+    }
     public class EmailTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class {
         public EmailTokenProvider();
         public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
         public override Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user);
     }
     public class ExternalLoginInfo : UserLoginInfo {
         public ExternalLoginInfo(ClaimsPrincipal principal, string loginProvider, string providerKey, string displayName);
         public IEnumerable<AuthenticationToken> AuthenticationTokens { get; set; }
         public ClaimsPrincipal Principal { get; set; }
     }
     public class IdentityBuilder {
         public IdentityBuilder(Type user, IServiceCollection services);
         public IdentityBuilder(Type user, Type role, IServiceCollection services);
         public Type RoleType { get; private set; }
         public IServiceCollection Services { get; private set; }
         public Type UserType { get; private set; }
         public virtual IdentityBuilder AddClaimsPrincipalFactory<TFactory>() where TFactory : class;
         public virtual IdentityBuilder AddErrorDescriber<TDescriber>() where TDescriber : IdentityErrorDescriber;
         public virtual IdentityBuilder AddPasswordValidator<TValidator>() where TValidator : class;
         public virtual IdentityBuilder AddPersonalDataProtection<TProtector, TKeyRing>() where TProtector : class, ILookupProtector where TKeyRing : class, ILookupProtectorKeyRing;
         public virtual IdentityBuilder AddRoleManager<TRoleManager>() where TRoleManager : class;
         public virtual IdentityBuilder AddRoles<TRole>() where TRole : class;
         public virtual IdentityBuilder AddRoleStore<TStore>() where TStore : class;
         public virtual IdentityBuilder AddRoleValidator<TRole>() where TRole : class;
         public virtual IdentityBuilder AddTokenProvider(string providerName, Type provider);
         public virtual IdentityBuilder AddTokenProvider<TProvider>(string providerName) where TProvider : class;
         public virtual IdentityBuilder AddUserManager<TUserManager>() where TUserManager : class;
         public virtual IdentityBuilder AddUserStore<TStore>() where TStore : class;
         public virtual IdentityBuilder AddUserValidator<TValidator>() where TValidator : class;
     }
     public static class IdentityBuilderExtensions {
         public static IdentityBuilder AddDefaultTokenProviders(this IdentityBuilder builder);
         public static IdentityBuilder AddSignInManager(this IdentityBuilder builder);
         public static IdentityBuilder AddSignInManager<TSignInManager>(this IdentityBuilder builder) where TSignInManager : class;
     }
-    public static class IdentityBuilderUIExtensions {
 {
-        public static IdentityBuilder AddDefaultUI(this IdentityBuilder builder);

-        public static IdentityBuilder AddDefaultUI(this IdentityBuilder builder, UIFramework framework);

-    }
     public class IdentityConstants {
         public static readonly string ApplicationScheme;
         public static readonly string ExternalScheme;
         public static readonly string TwoFactorRememberMeScheme;
         public static readonly string TwoFactorUserIdScheme;
         public IdentityConstants();
     }
     public static class IdentityCookieAuthenticationBuilderExtensions {
         public static OptionsBuilder<CookieAuthenticationOptions> AddApplicationCookie(this AuthenticationBuilder builder);
         public static OptionsBuilder<CookieAuthenticationOptions> AddExternalCookie(this AuthenticationBuilder builder);
         public static IdentityCookiesBuilder AddIdentityCookies(this AuthenticationBuilder builder);
         public static IdentityCookiesBuilder AddIdentityCookies(this AuthenticationBuilder builder, Action<IdentityCookiesBuilder> configureCookies);
         public static OptionsBuilder<CookieAuthenticationOptions> AddTwoFactorRememberMeCookie(this AuthenticationBuilder builder);
         public static OptionsBuilder<CookieAuthenticationOptions> AddTwoFactorUserIdCookie(this AuthenticationBuilder builder);
     }
     public class IdentityCookiesBuilder {
         public IdentityCookiesBuilder();
         public OptionsBuilder<CookieAuthenticationOptions> ApplicationCookie { get; set; }
         public OptionsBuilder<CookieAuthenticationOptions> ExternalCookie { get; set; }
         public OptionsBuilder<CookieAuthenticationOptions> TwoFactorRememberMeCookie { get; set; }
         public OptionsBuilder<CookieAuthenticationOptions> TwoFactorUserIdCookie { get; set; }
     }
     public class IdentityError {
         public IdentityError();
         public string Code { get; set; }
         public string Description { get; set; }
     }
     public class IdentityErrorDescriber {
         public IdentityErrorDescriber();
         public virtual IdentityError ConcurrencyFailure();
         public virtual IdentityError DefaultError();
         public virtual IdentityError DuplicateEmail(string email);
         public virtual IdentityError DuplicateRoleName(string role);
         public virtual IdentityError DuplicateUserName(string userName);
         public virtual IdentityError InvalidEmail(string email);
         public virtual IdentityError InvalidRoleName(string role);
         public virtual IdentityError InvalidToken();
         public virtual IdentityError InvalidUserName(string userName);
         public virtual IdentityError LoginAlreadyAssociated();
         public virtual IdentityError PasswordMismatch();
         public virtual IdentityError PasswordRequiresDigit();
         public virtual IdentityError PasswordRequiresLower();
         public virtual IdentityError PasswordRequiresNonAlphanumeric();
         public virtual IdentityError PasswordRequiresUniqueChars(int uniqueChars);
         public virtual IdentityError PasswordRequiresUpper();
         public virtual IdentityError PasswordTooShort(int length);
         public virtual IdentityError RecoveryCodeRedemptionFailed();
         public virtual IdentityError UserAlreadyHasPassword();
         public virtual IdentityError UserAlreadyInRole(string role);
         public virtual IdentityError UserLockoutNotEnabled();
         public virtual IdentityError UserNotInRole(string role);
     }
     public class IdentityOptions {
         public IdentityOptions();
         public ClaimsIdentityOptions ClaimsIdentity { get; set; }
         public LockoutOptions Lockout { get; set; }
         public PasswordOptions Password { get; set; }
         public SignInOptions SignIn { get; set; }
         public StoreOptions Stores { get; set; }
         public TokenOptions Tokens { get; set; }
         public UserOptions User { get; set; }
     }
     public class IdentityResult {
         public IdentityResult();
         public IEnumerable<IdentityError> Errors { get; }
         public bool Succeeded { get; protected set; }
         public static IdentityResult Success { get; }
         public static IdentityResult Failed(params IdentityError[] errors);
         public override string ToString();
     }
     public class IdentityRole : IdentityRole<string> {
         public IdentityRole();
         public IdentityRole(string roleName);
     }
     public class IdentityRole<TKey> where TKey : IEquatable<TKey> {
         public IdentityRole();
         public IdentityRole(string roleName);
         public virtual string ConcurrencyStamp { get; set; }
         public virtual TKey Id { get; set; }
         public virtual string Name { get; set; }
         public virtual string NormalizedName { get; set; }
         public override string ToString();
     }
     public class IdentityRoleClaim<TKey> where TKey : IEquatable<TKey> {
         public IdentityRoleClaim();
         public virtual string ClaimType { get; set; }
         public virtual string ClaimValue { get; set; }
         public virtual int Id { get; set; }
         public virtual TKey RoleId { get; set; }
         public virtual void InitializeFromClaim(Claim other);
         public virtual Claim ToClaim();
     }
     public class IdentityUser : IdentityUser<string> {
         public IdentityUser();
         public IdentityUser(string userName);
     }
     public class IdentityUser<TKey> where TKey : IEquatable<TKey> {
         public IdentityUser();
         public IdentityUser(string userName);
         public virtual int AccessFailedCount { get; set; }
         public virtual string ConcurrencyStamp { get; set; }
         public virtual string Email { get; set; }
         public virtual bool EmailConfirmed { get; set; }
         public virtual TKey Id { get; set; }
         public virtual bool LockoutEnabled { get; set; }
         public virtual Nullable<DateTimeOffset> LockoutEnd { get; set; }
         public virtual string NormalizedEmail { get; set; }
         public virtual string NormalizedUserName { get; set; }
         public virtual string PasswordHash { get; set; }
         public virtual string PhoneNumber { get; set; }
         public virtual bool PhoneNumberConfirmed { get; set; }
         public virtual string SecurityStamp { get; set; }
         public virtual bool TwoFactorEnabled { get; set; }
         public virtual string UserName { get; set; }
         public override string ToString();
     }
     public class IdentityUserClaim<TKey> where TKey : IEquatable<TKey> {
         public IdentityUserClaim();
         public virtual string ClaimType { get; set; }
         public virtual string ClaimValue { get; set; }
         public virtual int Id { get; set; }
         public virtual TKey UserId { get; set; }
         public virtual void InitializeFromClaim(Claim claim);
         public virtual Claim ToClaim();
     }
     public class IdentityUserLogin<TKey> where TKey : IEquatable<TKey> {
         public IdentityUserLogin();
         public virtual string LoginProvider { get; set; }
         public virtual string ProviderDisplayName { get; set; }
         public virtual string ProviderKey { get; set; }
         public virtual TKey UserId { get; set; }
     }
     public class IdentityUserRole<TKey> where TKey : IEquatable<TKey> {
         public IdentityUserRole();
         public virtual TKey RoleId { get; set; }
         public virtual TKey UserId { get; set; }
     }
     public class IdentityUserToken<TKey> where TKey : IEquatable<TKey> {
         public IdentityUserToken();
         public virtual string LoginProvider { get; set; }
         public virtual string Name { get; set; }
         public virtual TKey UserId { get; set; }
         public virtual string Value { get; set; }
     }
     public interface ILookupNormalizer {
-        string Normalize(string key);

+        string NormalizeEmail(string email);
+        string NormalizeName(string name);
     }
     public interface ILookupProtector {
         string Protect(string keyId, string data);
         string Unprotect(string keyId, string data);
     }
     public interface ILookupProtectorKeyRing {
         string CurrentKeyId { get; }
         string this[string keyId] { get; }
         IEnumerable<string> GetAllKeyIds();
     }
     public interface IPasswordHasher<TUser> where TUser : class {
         string HashPassword(TUser user, string password);
         PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword);
     }
     public interface IPasswordValidator<TUser> where TUser : class {
         Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password);
     }
     public interface IPersonalDataProtector {
         string Protect(string data);
         string Unprotect(string data);
     }
     public interface IProtectedUserStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class
     public interface IQueryableRoleStore<TRole> : IDisposable, IRoleStore<TRole> where TRole : class {
         IQueryable<TRole> Roles { get; }
     }
     public interface IQueryableUserStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         IQueryable<TUser> Users { get; }
     }
     public interface IRoleClaimStore<TRole> : IDisposable, IRoleStore<TRole> where TRole : class {
         Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken));
         Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
         Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken));
     }
     public interface IRoleStore<TRole> : IDisposable where TRole : class {
         Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken);
         Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken);
         Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken);
         Task<TRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken);
         Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken);
         Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken);
         Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken);
         Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken);
         Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken);
         Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken);
     }
     public interface IRoleValidator<TRole> where TRole : class {
         Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role);
     }
     public interface ISecurityStampValidator {
         Task ValidateAsync(CookieValidatePrincipalContext context);
     }
     public interface ITwoFactorSecurityStampValidator : ISecurityStampValidator
     public interface IUserAuthenticationTokenStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<string> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken);
         Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken);
         Task SetTokenAsync(TUser user, string loginProvider, string name, string value, CancellationToken cancellationToken);
     }
     public interface IUserAuthenticatorKeyStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<string> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken);
         Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken);
     }
     public interface IUserClaimsPrincipalFactory<TUser> where TUser : class {
         Task<ClaimsPrincipal> CreateAsync(TUser user);
     }
     public interface IUserClaimStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken);
         Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken);
         Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken);
         Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken);
         Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken);
     }
+    public interface IUserConfirmation<TUser> where TUser : class {
+        Task<bool> IsConfirmedAsync(UserManager<TUser> manager, TUser user);
+    }
     public interface IUserEmailStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
         Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken);
         Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken);
         Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken);
         Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken);
         Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken);
         Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken);
     }
     public interface IUserLockoutStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken);
         Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken);
         Task<Nullable<DateTimeOffset>> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken);
         Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken);
         Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken);
         Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken);
         Task SetLockoutEndDateAsync(TUser user, Nullable<DateTimeOffset> lockoutEnd, CancellationToken cancellationToken);
     }
     public interface IUserLoginStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken);
         Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken);
         Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken);
         Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken);
     }
     public interface IUserPasswordStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken);
         Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken);
         Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken);
     }
     public interface IUserPhoneNumberStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken);
         Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken);
         Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken);
         Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken);
     }
     public interface IUserRoleStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken);
         Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken);
         Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken);
         Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken);
         Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken);
     }
     public interface IUserSecurityStampStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken);
         Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken);
     }
     public interface IUserStore<TUser> : IDisposable where TUser : class {
         Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken);
         Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken);
         Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken);
         Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken);
         Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken);
         Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken);
         Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken);
         Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken);
         Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken);
         Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken);
     }
     public interface IUserTwoFactorRecoveryCodeStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken);
         Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken);
         Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken);
     }
     public interface IUserTwoFactorStore<TUser> : IDisposable, IUserStore<TUser> where TUser : class {
         Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken);
         Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken);
     }
     public interface IUserTwoFactorTokenProvider<TUser> where TUser : class {
         Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
         Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user);
         Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user);
     }
     public interface IUserValidator<TUser> where TUser : class {
         Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user);
     }
     public class LockoutOptions {
         public LockoutOptions();
         public bool AllowedForNewUsers { get; set; }
         public TimeSpan DefaultLockoutTimeSpan { get; set; }
         public int MaxFailedAccessAttempts { get; set; }
     }
     public class PasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class {
         public PasswordHasher(IOptions<PasswordHasherOptions> optionsAccessor = null);
         public virtual string HashPassword(TUser user, string password);
         public virtual PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword);
     }
     public enum PasswordHasherCompatibilityMode {
         IdentityV2 = 0,
         IdentityV3 = 1,
     }
     public class PasswordHasherOptions {
         public PasswordHasherOptions();
         public PasswordHasherCompatibilityMode CompatibilityMode { get; set; }
         public int IterationCount { get; set; }
     }
     public class PasswordOptions {
         public PasswordOptions();
         public bool RequireDigit { get; set; }
         public int RequiredLength { get; set; }
         public int RequiredUniqueChars { get; set; }
         public bool RequireLowercase { get; set; }
         public bool RequireNonAlphanumeric { get; set; }
         public bool RequireUppercase { get; set; }
     }
     public class PasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : class {
         public PasswordValidator(IdentityErrorDescriber errors = null);
         public IdentityErrorDescriber Describer { get; private set; }
         public virtual bool IsDigit(char c);
         public virtual bool IsLetterOrDigit(char c);
         public virtual bool IsLower(char c);
         public virtual bool IsUpper(char c);
         public virtual Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password);
     }
     public enum PasswordVerificationResult {
         Failed = 0,
         Success = 1,
         SuccessRehashNeeded = 2,
     }
     public class PersonalDataAttribute : Attribute {
         public PersonalDataAttribute();
     }
     public class PhoneNumberTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class {
         public PhoneNumberTokenProvider();
         public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
         public override Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user);
     }
     public class ProtectedPersonalDataAttribute : PersonalDataAttribute {
         public ProtectedPersonalDataAttribute();
     }
     public class RoleManager<TRole> : IDisposable where TRole : class {
         public RoleManager(IRoleStore<TRole> store, IEnumerable<IRoleValidator<TRole>> roleValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, ILogger<RoleManager<TRole>> logger);
         protected virtual CancellationToken CancellationToken { get; }
         public IdentityErrorDescriber ErrorDescriber { get; set; }
         public ILookupNormalizer KeyNormalizer { get; set; }
         public virtual ILogger Logger { get; set; }
         public virtual IQueryable<TRole> Roles { get; }
         public IList<IRoleValidator<TRole>> RoleValidators { get; }
         protected IRoleStore<TRole> Store { get; private set; }
         public virtual bool SupportsQueryableRoles { get; }
         public virtual bool SupportsRoleClaims { get; }
         public virtual Task<IdentityResult> AddClaimAsync(TRole role, Claim claim);
         public virtual Task<IdentityResult> CreateAsync(TRole role);
         public virtual Task<IdentityResult> DeleteAsync(TRole role);
         public void Dispose();
         protected virtual void Dispose(bool disposing);
         public virtual Task<TRole> FindByIdAsync(string roleId);
         public virtual Task<TRole> FindByNameAsync(string roleName);
         public virtual Task<IList<Claim>> GetClaimsAsync(TRole role);
         public virtual Task<string> GetRoleIdAsync(TRole role);
         public virtual Task<string> GetRoleNameAsync(TRole role);
         public virtual string NormalizeKey(string key);
         public virtual Task<IdentityResult> RemoveClaimAsync(TRole role, Claim claim);
         public virtual Task<bool> RoleExistsAsync(string roleName);
         public virtual Task<IdentityResult> SetRoleNameAsync(TRole role, string name);
         protected void ThrowIfDisposed();
         public virtual Task<IdentityResult> UpdateAsync(TRole role);
         public virtual Task UpdateNormalizedRoleNameAsync(TRole role);
         protected virtual Task<IdentityResult> UpdateRoleAsync(TRole role);
         protected virtual Task<IdentityResult> ValidateRoleAsync(TRole role);
     }
     public abstract class RoleStoreBase<TRole, TKey, TUserRole, TRoleClaim> : IDisposable, IQueryableRoleStore<TRole>, IRoleClaimStore<TRole>, IRoleStore<TRole> where TRole : IdentityRole<TKey> where TKey : IEquatable<TKey> where TUserRole : IdentityUserRole<TKey>, new() where TRoleClaim : IdentityRoleClaim<TKey>, new() {
         public RoleStoreBase(IdentityErrorDescriber describer);
         public IdentityErrorDescriber ErrorDescriber { get; set; }
         public abstract IQueryable<TRole> Roles { get; }
         public abstract Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken));
         public virtual TKey ConvertIdFromString(string id);
         public virtual string ConvertIdToString(TKey id);
         public abstract Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
         protected virtual TRoleClaim CreateRoleClaim(TRole role, Claim claim);
         public abstract Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
         public void Dispose();
         public abstract Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken));
         protected void ThrowIfDisposed();
         public abstract Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken));
     }
     public class RoleValidator<TRole> : IRoleValidator<TRole> where TRole : class {
         public RoleValidator(IdentityErrorDescriber errors = null);
         public virtual Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role);
     }
     public class SecurityStampRefreshingPrincipalContext {
         public SecurityStampRefreshingPrincipalContext();
         public ClaimsPrincipal CurrentPrincipal { get; set; }
         public ClaimsPrincipal NewPrincipal { get; set; }
     }
     public static class SecurityStampValidator {
         public static Task ValidateAsync<TValidator>(CookieValidatePrincipalContext context) where TValidator : ISecurityStampValidator;
         public static Task ValidatePrincipalAsync(CookieValidatePrincipalContext context);
     }
     public class SecurityStampValidator<TUser> : ISecurityStampValidator where TUser : class {
-        public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock);

+        public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock, ILoggerFactory logger);
         public ISystemClock Clock { get; }
+        public ILogger Logger { get; set; }
         public SecurityStampValidatorOptions Options { get; }
         public SignInManager<TUser> SignInManager { get; }
         protected virtual Task SecurityStampVerified(TUser user, CookieValidatePrincipalContext context);
         public virtual Task ValidateAsync(CookieValidatePrincipalContext context);
         protected virtual Task<TUser> VerifySecurityStamp(ClaimsPrincipal principal);
     }
     public class SecurityStampValidatorOptions {
         public SecurityStampValidatorOptions();
         public Func<SecurityStampRefreshingPrincipalContext, Task> OnRefreshingPrincipal { get; set; }
         public TimeSpan ValidationInterval { get; set; }
     }
     public class SignInManager<TUser> where TUser : class {
-        public SignInManager(UserManager<TUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<TUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<TUser>> logger, IAuthenticationSchemeProvider schemes);

+        public SignInManager(UserManager<TUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<TUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<TUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<TUser> confirmation);
         public IUserClaimsPrincipalFactory<TUser> ClaimsFactory { get; set; }
         public HttpContext Context { get; set; }
         public virtual ILogger Logger { get; set; }
         public IdentityOptions Options { get; set; }
         public UserManager<TUser> UserManager { get; set; }
         public virtual Task<bool> CanSignInAsync(TUser user);
         public virtual Task<SignInResult> CheckPasswordSignInAsync(TUser user, string password, bool lockoutOnFailure);
         public virtual AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl, string userId = null);
         public virtual Task<ClaimsPrincipal> CreateUserPrincipalAsync(TUser user);
         public virtual Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent);
         public virtual Task<SignInResult> ExternalLoginSignInAsync(string loginProvider, string providerKey, bool isPersistent, bool bypassTwoFactor);
         public virtual Task ForgetTwoFactorClientAsync();
         public virtual Task<IEnumerable<AuthenticationScheme>> GetExternalAuthenticationSchemesAsync();
         public virtual Task<ExternalLoginInfo> GetExternalLoginInfoAsync(string expectedXsrf = null);
         public virtual Task<TUser> GetTwoFactorAuthenticationUserAsync();
         protected virtual Task<bool> IsLockedOut(TUser user);
         public virtual bool IsSignedIn(ClaimsPrincipal principal);
         public virtual Task<bool> IsTwoFactorClientRememberedAsync(TUser user);
         protected virtual Task<SignInResult> LockedOut(TUser user);
         public virtual Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure);
         public virtual Task<SignInResult> PasswordSignInAsync(TUser user, string password, bool isPersistent, bool lockoutOnFailure);
         protected virtual Task<SignInResult> PreSignInCheck(TUser user);
         public virtual Task RefreshSignInAsync(TUser user);
         public virtual Task RememberTwoFactorClientAsync(TUser user);
         protected virtual Task ResetLockout(TUser user);
         public virtual Task SignInAsync(TUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null);
         public virtual Task SignInAsync(TUser user, bool isPersistent, string authenticationMethod = null);
         protected virtual Task<SignInResult> SignInOrTwoFactorAsync(TUser user, bool isPersistent, string loginProvider = null, bool bypassTwoFactor = false);
+        public virtual Task SignInWithClaimsAsync(TUser user, AuthenticationProperties authenticationProperties, IEnumerable<Claim> additionalClaims);
+        public virtual Task SignInWithClaimsAsync(TUser user, bool isPersistent, IEnumerable<Claim> additionalClaims);
         public virtual Task SignOutAsync();
         public virtual Task<SignInResult> TwoFactorAuthenticatorSignInAsync(string code, bool isPersistent, bool rememberClient);
         public virtual Task<SignInResult> TwoFactorRecoveryCodeSignInAsync(string recoveryCode);
         public virtual Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool isPersistent, bool rememberClient);
         public virtual Task<IdentityResult> UpdateExternalAuthenticationTokensAsync(ExternalLoginInfo externalLogin);
         public virtual Task<TUser> ValidateSecurityStampAsync(ClaimsPrincipal principal);
         public virtual Task<bool> ValidateSecurityStampAsync(TUser user, string securityStamp);
         public virtual Task<TUser> ValidateTwoFactorSecurityStampAsync(ClaimsPrincipal principal);
     }
     public class SignInOptions {
         public SignInOptions();
+        public bool RequireConfirmedAccount { get; set; }
         public bool RequireConfirmedEmail { get; set; }
         public bool RequireConfirmedPhoneNumber { get; set; }
     }
     public class SignInResult {
         public SignInResult();
         public static SignInResult Failed { get; }
         public bool IsLockedOut { get; protected set; }
         public bool IsNotAllowed { get; protected set; }
         public static SignInResult LockedOut { get; }
         public static SignInResult NotAllowed { get; }
         public bool RequiresTwoFactor { get; protected set; }
         public bool Succeeded { get; protected set; }
         public static SignInResult Success { get; }
         public static SignInResult TwoFactorRequired { get; }
         public override string ToString();
     }
     public class StoreOptions {
         public StoreOptions();
         public int MaxLengthForKeys { get; set; }
         public bool ProtectPersonalData { get; set; }
     }
     public class TokenOptions {
         public static readonly string DefaultAuthenticatorProvider;
         public static readonly string DefaultEmailProvider;
         public static readonly string DefaultPhoneProvider;
         public static readonly string DefaultProvider;
         public TokenOptions();
         public string AuthenticatorIssuer { get; set; }
         public string AuthenticatorTokenProvider { get; set; }
         public string ChangeEmailTokenProvider { get; set; }
         public string ChangePhoneNumberTokenProvider { get; set; }
         public string EmailConfirmationTokenProvider { get; set; }
         public string PasswordResetTokenProvider { get; set; }
         public Dictionary<string, TokenProviderDescriptor> ProviderMap { get; set; }
     }
     public class TokenProviderDescriptor {
         public TokenProviderDescriptor(Type type);
         public object ProviderInstance { get; set; }
         public Type ProviderType { get; }
     }
     public abstract class TotpSecurityStampBasedTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser> where TUser : class {
         protected TotpSecurityStampBasedTokenProvider();
         public abstract Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user);
         public virtual Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user);
         public virtual Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user);
         public virtual Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user);
     }
     public class TwoFactorSecurityStampValidator<TUser> : SecurityStampValidator<TUser>, ISecurityStampValidator, ITwoFactorSecurityStampValidator where TUser : class {
-        public TwoFactorSecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock);

+        public TwoFactorSecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock, ILoggerFactory logger);
         protected override Task SecurityStampVerified(TUser user, CookieValidatePrincipalContext context);
         protected override Task<TUser> VerifySecurityStamp(ClaimsPrincipal principal);
     }
-    public class UpperInvariantLookupNormalizer : ILookupNormalizer {
+    public sealed class UpperInvariantLookupNormalizer : ILookupNormalizer {
         public UpperInvariantLookupNormalizer();
-        public virtual string Normalize(string key);

+        public string NormalizeEmail(string email);
+        public string NormalizeName(string name);
     }
     public class UserClaimsPrincipalFactory<TUser> : IUserClaimsPrincipalFactory<TUser> where TUser : class {
         public UserClaimsPrincipalFactory(UserManager<TUser> userManager, IOptions<IdentityOptions> optionsAccessor);
         public IdentityOptions Options { get; private set; }
         public UserManager<TUser> UserManager { get; private set; }
         public virtual Task<ClaimsPrincipal> CreateAsync(TUser user);
         protected virtual Task<ClaimsIdentity> GenerateClaimsAsync(TUser user);
     }
     public class UserClaimsPrincipalFactory<TUser, TRole> : UserClaimsPrincipalFactory<TUser> where TUser : class where TRole : class {
         public UserClaimsPrincipalFactory(UserManager<TUser> userManager, RoleManager<TRole> roleManager, IOptions<IdentityOptions> options);
         public RoleManager<TRole> RoleManager { get; private set; }
         protected override Task<ClaimsIdentity> GenerateClaimsAsync(TUser user);
     }
     public class UserLoginInfo {
         public UserLoginInfo(string loginProvider, string providerKey, string displayName);
         public string LoginProvider { get; set; }
         public string ProviderDisplayName { get; set; }
         public string ProviderKey { get; set; }
     }
     public class UserManager<TUser> : IDisposable where TUser : class {
         public const string ChangePhoneNumberTokenPurpose = "ChangePhoneNumber";
         public const string ConfirmEmailTokenPurpose = "EmailConfirmation";
         public const string ResetPasswordTokenPurpose = "ResetPassword";
         public UserManager(IUserStore<TUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<TUser> passwordHasher, IEnumerable<IUserValidator<TUser>> userValidators, IEnumerable<IPasswordValidator<TUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<TUser>> logger);
         protected virtual CancellationToken CancellationToken { get; }
         public IdentityErrorDescriber ErrorDescriber { get; set; }
         public ILookupNormalizer KeyNormalizer { get; set; }
         public virtual ILogger Logger { get; set; }
         public IdentityOptions Options { get; set; }
         public IPasswordHasher<TUser> PasswordHasher { get; set; }
         public IList<IPasswordValidator<TUser>> PasswordValidators { get; }
         protected internal IUserStore<TUser> Store { get; set; }
         public virtual bool SupportsQueryableUsers { get; }
         public virtual bool SupportsUserAuthenticationTokens { get; }
         public virtual bool SupportsUserAuthenticatorKey { get; }
         public virtual bool SupportsUserClaim { get; }
         public virtual bool SupportsUserEmail { get; }
         public virtual bool SupportsUserLockout { get; }
         public virtual bool SupportsUserLogin { get; }
         public virtual bool SupportsUserPassword { get; }
         public virtual bool SupportsUserPhoneNumber { get; }
         public virtual bool SupportsUserRole { get; }
         public virtual bool SupportsUserSecurityStamp { get; }
         public virtual bool SupportsUserTwoFactor { get; }
         public virtual bool SupportsUserTwoFactorRecoveryCodes { get; }
         public virtual IQueryable<TUser> Users { get; }
         public IList<IUserValidator<TUser>> UserValidators { get; }
         public virtual Task<IdentityResult> AccessFailedAsync(TUser user);
         public virtual Task<IdentityResult> AddClaimAsync(TUser user, Claim claim);
         public virtual Task<IdentityResult> AddClaimsAsync(TUser user, IEnumerable<Claim> claims);
         public virtual Task<IdentityResult> AddLoginAsync(TUser user, UserLoginInfo login);
         public virtual Task<IdentityResult> AddPasswordAsync(TUser user, string password);
         public virtual Task<IdentityResult> AddToRoleAsync(TUser user, string role);
         public virtual Task<IdentityResult> AddToRolesAsync(TUser user, IEnumerable<string> roles);
         public virtual Task<IdentityResult> ChangeEmailAsync(TUser user, string newEmail, string token);
         public virtual Task<IdentityResult> ChangePasswordAsync(TUser user, string currentPassword, string newPassword);
         public virtual Task<IdentityResult> ChangePhoneNumberAsync(TUser user, string phoneNumber, string token);
         public virtual Task<bool> CheckPasswordAsync(TUser user, string password);
         public virtual Task<IdentityResult> ConfirmEmailAsync(TUser user, string token);
         public virtual Task<int> CountRecoveryCodesAsync(TUser user);
         public virtual Task<IdentityResult> CreateAsync(TUser user);
         public virtual Task<IdentityResult> CreateAsync(TUser user, string password);
         public virtual Task<byte[]> CreateSecurityTokenAsync(TUser user);
         protected virtual string CreateTwoFactorRecoveryCode();
         public virtual Task<IdentityResult> DeleteAsync(TUser user);
         public void Dispose();
         protected virtual void Dispose(bool disposing);
         public virtual Task<TUser> FindByEmailAsync(string email);
         public virtual Task<TUser> FindByIdAsync(string userId);
         public virtual Task<TUser> FindByLoginAsync(string loginProvider, string providerKey);
         public virtual Task<TUser> FindByNameAsync(string userName);
         public virtual Task<string> GenerateChangeEmailTokenAsync(TUser user, string newEmail);
         public virtual Task<string> GenerateChangePhoneNumberTokenAsync(TUser user, string phoneNumber);
         public virtual Task<string> GenerateConcurrencyStampAsync(TUser user);
         public virtual Task<string> GenerateEmailConfirmationTokenAsync(TUser user);
         public virtual string GenerateNewAuthenticatorKey();
         public virtual Task<IEnumerable<string>> GenerateNewTwoFactorRecoveryCodesAsync(TUser user, int number);
         public virtual Task<string> GeneratePasswordResetTokenAsync(TUser user);
         public virtual Task<string> GenerateTwoFactorTokenAsync(TUser user, string tokenProvider);
         public virtual Task<string> GenerateUserTokenAsync(TUser user, string tokenProvider, string purpose);
         public virtual Task<int> GetAccessFailedCountAsync(TUser user);
         public virtual Task<string> GetAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName);
         public virtual Task<string> GetAuthenticatorKeyAsync(TUser user);
         protected static string GetChangeEmailTokenPurpose(string newEmail);
         public virtual Task<IList<Claim>> GetClaimsAsync(TUser user);
         public virtual Task<string> GetEmailAsync(TUser user);
         public virtual Task<bool> GetLockoutEnabledAsync(TUser user);
         public virtual Task<Nullable<DateTimeOffset>> GetLockoutEndDateAsync(TUser user);
         public virtual Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user);
         public virtual Task<string> GetPhoneNumberAsync(TUser user);
         public virtual Task<IList<string>> GetRolesAsync(TUser user);
         public virtual Task<string> GetSecurityStampAsync(TUser user);
         public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user);
         public virtual Task<TUser> GetUserAsync(ClaimsPrincipal principal);
         public virtual string GetUserId(ClaimsPrincipal principal);
         public virtual Task<string> GetUserIdAsync(TUser user);
         public virtual string GetUserName(ClaimsPrincipal principal);
         public virtual Task<string> GetUserNameAsync(TUser user);
         public virtual Task<IList<TUser>> GetUsersForClaimAsync(Claim claim);
         public virtual Task<IList<TUser>> GetUsersInRoleAsync(string roleName);
         public virtual Task<IList<string>> GetValidTwoFactorProvidersAsync(TUser user);
         public virtual Task<bool> HasPasswordAsync(TUser user);
         public virtual Task<bool> IsEmailConfirmedAsync(TUser user);
         public virtual Task<bool> IsInRoleAsync(TUser user, string role);
         public virtual Task<bool> IsLockedOutAsync(TUser user);
         public virtual Task<bool> IsPhoneNumberConfirmedAsync(TUser user);
+        public virtual string NormalizeEmail(string email);
-        public virtual string NormalizeKey(string key);

+        public virtual string NormalizeName(string name);
         public virtual Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(TUser user, string code);
         public virtual void RegisterTokenProvider(string providerName, IUserTwoFactorTokenProvider<TUser> provider);
         public virtual Task<IdentityResult> RemoveAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName);
         public virtual Task<IdentityResult> RemoveClaimAsync(TUser user, Claim claim);
         public virtual Task<IdentityResult> RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims);
         public virtual Task<IdentityResult> RemoveFromRoleAsync(TUser user, string role);
         public virtual Task<IdentityResult> RemoveFromRolesAsync(TUser user, IEnumerable<string> roles);
         public virtual Task<IdentityResult> RemoveLoginAsync(TUser user, string loginProvider, string providerKey);
         public virtual Task<IdentityResult> RemovePasswordAsync(TUser user);
         public virtual Task<IdentityResult> ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim);
         public virtual Task<IdentityResult> ResetAccessFailedCountAsync(TUser user);
         public virtual Task<IdentityResult> ResetAuthenticatorKeyAsync(TUser user);
         public virtual Task<IdentityResult> ResetPasswordAsync(TUser user, string token, string newPassword);
         public virtual Task<IdentityResult> SetAuthenticationTokenAsync(TUser user, string loginProvider, string tokenName, string tokenValue);
         public virtual Task<IdentityResult> SetEmailAsync(TUser user, string email);
         public virtual Task<IdentityResult> SetLockoutEnabledAsync(TUser user, bool enabled);
         public virtual Task<IdentityResult> SetLockoutEndDateAsync(TUser user, Nullable<DateTimeOffset> lockoutEnd);
         public virtual Task<IdentityResult> SetPhoneNumberAsync(TUser user, string phoneNumber);
         public virtual Task<IdentityResult> SetTwoFactorEnabledAsync(TUser user, bool enabled);
         public virtual Task<IdentityResult> SetUserNameAsync(TUser user, string userName);
         protected void ThrowIfDisposed();
         public virtual Task<IdentityResult> UpdateAsync(TUser user);
         public virtual Task UpdateNormalizedEmailAsync(TUser user);
         public virtual Task UpdateNormalizedUserNameAsync(TUser user);
         protected virtual Task<IdentityResult> UpdatePasswordHash(TUser user, string newPassword, bool validatePassword);
         public virtual Task<IdentityResult> UpdateSecurityStampAsync(TUser user);
         protected virtual Task<IdentityResult> UpdateUserAsync(TUser user);
         protected Task<IdentityResult> ValidatePasswordAsync(TUser user, string password);
         protected Task<IdentityResult> ValidateUserAsync(TUser user);
         public virtual Task<bool> VerifyChangePhoneNumberTokenAsync(TUser user, string token, string phoneNumber);
         protected virtual Task<PasswordVerificationResult> VerifyPasswordAsync(IUserPasswordStore<TUser> store, TUser user, string password);
         public virtual Task<bool> VerifyTwoFactorTokenAsync(TUser user, string tokenProvider, string token);
         public virtual Task<bool> VerifyUserTokenAsync(TUser user, string tokenProvider, string purpose, string token);
     }
     public class UserOptions {
         public UserOptions();
         public string AllowedUserNameCharacters { get; set; }
         public bool RequireUniqueEmail { get; set; }
     }
     public abstract class UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken> : IDisposable, IQueryableUserStore<TUser>, IUserAuthenticationTokenStore<TUser>, IUserAuthenticatorKeyStore<TUser>, IUserClaimStore<TUser>, IUserEmailStore<TUser>, IUserLockoutStore<TUser>, IUserLoginStore<TUser>, IUserPasswordStore<TUser>, IUserPhoneNumberStore<TUser>, IUserSecurityStampStore<TUser>, IUserStore<TUser>, IUserTwoFactorRecoveryCodeStore<TUser>, IUserTwoFactorStore<TUser> where TUser : IdentityUser<TKey> where TKey : IEquatable<TKey> where TUserClaim : IdentityUserClaim<TKey>, new() where TUserLogin : IdentityUserLogin<TKey>, new() where TUserToken : IdentityUserToken<TKey>, new() {
         public UserStoreBase(IdentityErrorDescriber describer);
         public IdentityErrorDescriber ErrorDescriber { get; set; }
         public abstract IQueryable<TUser> Users { get; }
         public abstract Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken));
         protected abstract Task AddUserTokenAsync(TUserToken token);
         public virtual TKey ConvertIdFromString(string id);
         public virtual string ConvertIdToString(TKey id);
         public virtual Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken);
         public abstract Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         protected virtual TUserClaim CreateUserClaim(TUser user, Claim claim);
         protected virtual TUserLogin CreateUserLogin(TUser user, UserLoginInfo login);
         protected virtual TUserToken CreateUserToken(TUser user, string loginProvider, string name, string value);
         public abstract Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public void Dispose();
         public abstract Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken));
         protected abstract Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken);
         protected abstract Task<TUser> FindUserAsync(TKey userId, CancellationToken cancellationToken);
         protected abstract Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken);
         protected abstract Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken);
         public virtual Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken);
         public abstract Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<Nullable<DateTimeOffset>> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken);
         public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken);
         public abstract Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken);
         protected abstract Task RemoveUserTokenAsync(TUserToken token);
         public abstract Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken);
         public virtual Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken);
         public virtual Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetLockoutEndDateAsync(TUser user, Nullable<DateTimeOffset> lockoutEnd, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetTokenAsync(TUser user, string loginProvider, string name, string value, CancellationToken cancellationToken);
         public virtual Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken));
         public virtual Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken));
         protected void ThrowIfDisposed();
         public abstract Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
     }
     public abstract class UserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> : UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>, IDisposable, IUserRoleStore<TUser>, IUserStore<TUser> where TUser : IdentityUser<TKey> where TRole : IdentityRole<TKey> where TKey : IEquatable<TKey> where TUserClaim : IdentityUserClaim<TKey>, new() where TUserRole : IdentityUserRole<TKey>, new() where TUserLogin : IdentityUserLogin<TKey>, new() where TUserToken : IdentityUserToken<TKey>, new() where TRoleClaim : IdentityRoleClaim<TKey>, new() {
         public UserStoreBase(IdentityErrorDescriber describer);
         public abstract Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));
         protected virtual TUserRole CreateUserRole(TUser user, TRole role);
         protected abstract Task<TRole> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken);
         protected abstract Task<TUserRole> FindUserRoleAsync(TKey userId, TKey roleId, CancellationToken cancellationToken);
         public abstract Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));
         public abstract Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken));
     }
     public class UserValidator<TUser> : IUserValidator<TUser> where TUser : class {
         public UserValidator(IdentityErrorDescriber errors = null);
         public IdentityErrorDescriber Describer { get; private set; }
         public virtual Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user);
     }
 }
```

