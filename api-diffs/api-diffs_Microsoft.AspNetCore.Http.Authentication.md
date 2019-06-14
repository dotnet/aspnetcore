# Microsoft.AspNetCore.Http.Authentication

``` diff
-namespace Microsoft.AspNetCore.Http.Authentication {
 {
-    public class AuthenticateInfo {
 {
-        public AuthenticateInfo();

-        public AuthenticationDescription Description { get; set; }

-        public ClaimsPrincipal Principal { get; set; }

-        public AuthenticationProperties Properties { get; set; }

-    }
-    public class AuthenticationDescription {
 {
-        public AuthenticationDescription();

-        public AuthenticationDescription(IDictionary<string, object> items);

-        public string AuthenticationScheme { get; set; }

-        public string DisplayName { get; set; }

-        public IDictionary<string, object> Items { get; }

-    }
-    public abstract class AuthenticationManager {
 {
-        public const string AutomaticScheme = "Automatic";

-        protected AuthenticationManager();

-        public abstract HttpContext HttpContext { get; }

-        public abstract Task AuthenticateAsync(AuthenticateContext context);

-        public virtual Task<ClaimsPrincipal> AuthenticateAsync(string authenticationScheme);

-        public virtual Task ChallengeAsync();

-        public virtual Task ChallengeAsync(AuthenticationProperties properties);

-        public virtual Task ChallengeAsync(string authenticationScheme);

-        public virtual Task ChallengeAsync(string authenticationScheme, AuthenticationProperties properties);

-        public abstract Task ChallengeAsync(string authenticationScheme, AuthenticationProperties properties, ChallengeBehavior behavior);

-        public virtual Task ForbidAsync();

-        public virtual Task ForbidAsync(AuthenticationProperties properties);

-        public virtual Task ForbidAsync(string authenticationScheme);

-        public virtual Task ForbidAsync(string authenticationScheme, AuthenticationProperties properties);

-        public abstract Task<AuthenticateInfo> GetAuthenticateInfoAsync(string authenticationScheme);

-        public abstract IEnumerable<AuthenticationDescription> GetAuthenticationSchemes();

-        public virtual Task SignInAsync(string authenticationScheme, ClaimsPrincipal principal);

-        public abstract Task SignInAsync(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties);

-        public virtual Task SignOutAsync(string authenticationScheme);

-        public abstract Task SignOutAsync(string authenticationScheme, AuthenticationProperties properties);

-    }
-    public class AuthenticationProperties {
 {
-        public AuthenticationProperties();

-        public AuthenticationProperties(IDictionary<string, string> items);

-        public Nullable<bool> AllowRefresh { get; set; }

-        public Nullable<DateTimeOffset> ExpiresUtc { get; set; }

-        public bool IsPersistent { get; set; }

-        public Nullable<DateTimeOffset> IssuedUtc { get; set; }

-        public IDictionary<string, string> Items { get; }

-        public string RedirectUri { get; set; }

-    }
-}
```

