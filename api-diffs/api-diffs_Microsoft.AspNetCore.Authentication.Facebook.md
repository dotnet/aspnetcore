# Microsoft.AspNetCore.Authentication.Facebook

``` diff
-namespace Microsoft.AspNetCore.Authentication.Facebook {
 {
-    public static class FacebookDefaults {
 {
-        public const string AuthenticationScheme = "Facebook";

-        public static readonly string AuthorizationEndpoint;

-        public static readonly string DisplayName;

-        public static readonly string TokenEndpoint;

-        public static readonly string UserInformationEndpoint;

-    }
-    public class FacebookHandler : OAuthHandler<FacebookOptions> {
 {
-        public FacebookHandler(IOptionsMonitor<FacebookOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);

-        protected override Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens);

-        protected override string FormatScope();

-        protected override string FormatScope(IEnumerable<string> scopes);

-    }
-    public class FacebookOptions : OAuthOptions {
 {
-        public FacebookOptions();

-        public string AppId { get; set; }

-        public string AppSecret { get; set; }

-        public ICollection<string> Fields { get; }

-        public bool SendAppSecretProof { get; set; }

-        public override void Validate();

-    }
-}
```

