# Microsoft.AspNetCore.Http.Authentication.Internal

``` diff
-namespace Microsoft.AspNetCore.Http.Authentication.Internal {
 {
-    public class DefaultAuthenticationManager : AuthenticationManager {
 {
-        public DefaultAuthenticationManager(HttpContext context);

-        public override HttpContext HttpContext { get; }

-        public override Task AuthenticateAsync(AuthenticateContext context);

-        public override Task ChallengeAsync(string authenticationScheme, AuthenticationProperties properties, ChallengeBehavior behavior);

-        public override Task<AuthenticateInfo> GetAuthenticateInfoAsync(string authenticationScheme);

-        public override IEnumerable<AuthenticationDescription> GetAuthenticationSchemes();

-        public virtual void Initialize(HttpContext context);

-        public override Task SignInAsync(string authenticationScheme, ClaimsPrincipal principal, AuthenticationProperties properties);

-        public override Task SignOutAsync(string authenticationScheme, AuthenticationProperties properties);

-        public virtual void Uninitialize();

-    }
-}
```

