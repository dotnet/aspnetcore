# Microsoft.AspNetCore.Identity

``` diff
 namespace Microsoft.AspNetCore.Identity {
+    public class DefaultUserConfirmation<TUser> : IUserConfirmation<TUser> where TUser : class {
+        public DefaultUserConfirmation();
+        public virtual Task<bool> IsConfirmedAsync(UserManager<TUser> manager, TUser user);
+    }
-    public static class IdentityBuilderUIExtensions {
 {
-        public static IdentityBuilder AddDefaultUI(this IdentityBuilder builder);

-        public static IdentityBuilder AddDefaultUI(this IdentityBuilder builder, UIFramework framework);

-    }
     public interface ILookupNormalizer {
-        string Normalize(string key);

+        string NormalizeEmail(string email);
+        string NormalizeName(string name);
     }
+    public interface IUserConfirmation<TUser> where TUser : class {
+        Task<bool> IsConfirmedAsync(UserManager<TUser> manager, TUser user);
+    }
     public class SecurityStampValidator<TUser> : ISecurityStampValidator where TUser : class {
-        public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock);

+        public SecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock, ILoggerFactory logger);
+        public ILogger Logger { get; set; }
     }
     public class SignInManager<TUser> where TUser : class {
-        public SignInManager(UserManager<TUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<TUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<TUser>> logger, IAuthenticationSchemeProvider schemes);

+        public SignInManager(UserManager<TUser> userManager, IHttpContextAccessor contextAccessor, IUserClaimsPrincipalFactory<TUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor, ILogger<SignInManager<TUser>> logger, IAuthenticationSchemeProvider schemes, IUserConfirmation<TUser> confirmation);
+        public virtual Task SignInWithClaimsAsync(TUser user, AuthenticationProperties authenticationProperties, IEnumerable<Claim> additionalClaims);
+        public virtual Task SignInWithClaimsAsync(TUser user, bool isPersistent, IEnumerable<Claim> additionalClaims);
     }
     public class SignInOptions {
+        public bool RequireConfirmedAccount { get; set; }
     }
     public class TwoFactorSecurityStampValidator<TUser> : SecurityStampValidator<TUser>, ISecurityStampValidator, ITwoFactorSecurityStampValidator where TUser : class {
-        public TwoFactorSecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock);

+        public TwoFactorSecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, SignInManager<TUser> signInManager, ISystemClock clock, ILoggerFactory logger);
     }
-    public class UpperInvariantLookupNormalizer : ILookupNormalizer {
+    public sealed class UpperInvariantLookupNormalizer : ILookupNormalizer {
-        public virtual string Normalize(string key);

+        public string NormalizeEmail(string email);
+        public string NormalizeName(string name);
     }
     public class UserManager<TUser> : IDisposable where TUser : class {
+        public virtual string NormalizeEmail(string email);
-        public virtual string NormalizeKey(string key);

+        public virtual string NormalizeName(string name);
     }
 }
```

