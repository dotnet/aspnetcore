# Microsoft.AspNetCore.Authentication.Google

``` diff
-namespace Microsoft.AspNetCore.Authentication.Google {
 {
-    public class GoogleChallengeProperties : OAuthChallengeProperties {
 {
-        public static readonly string AccessTypeKey;

-        public static readonly string ApprovalPromptKey;

-        public static readonly string IncludeGrantedScopesKey;

-        public static readonly string LoginHintKey;

-        public static readonly string PromptParameterKey;

-        public GoogleChallengeProperties();

-        public GoogleChallengeProperties(IDictionary<string, string> items);

-        public GoogleChallengeProperties(IDictionary<string, string> items, IDictionary<string, object> parameters);

-        public string AccessType { get; set; }

-        public string ApprovalPrompt { get; set; }

-        public Nullable<bool> IncludeGrantedScopes { get; set; }

-        public string LoginHint { get; set; }

-        public string Prompt { get; set; }

-    }
-    public static class GoogleDefaults {
 {
-        public const string AuthenticationScheme = "Google";

-        public static readonly string AuthorizationEndpoint;

-        public static readonly string DisplayName;

-        public static readonly string TokenEndpoint;

-        public static readonly string UserInformationEndpoint;

-    }
-    public class GoogleHandler : OAuthHandler<GoogleOptions> {
 {
-        public GoogleHandler(IOptionsMonitor<GoogleOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);

-        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri);

-        protected override Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens);

-    }
-    public static class GoogleHelper {
 {
-        public static string GetEmail(JObject user);

-    }
-    public class GoogleOptions : OAuthOptions {
 {
-        public GoogleOptions();

-        public string AccessType { get; set; }

-    }
-}
```

