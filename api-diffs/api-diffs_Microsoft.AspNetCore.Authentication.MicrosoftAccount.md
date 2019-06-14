# Microsoft.AspNetCore.Authentication.MicrosoftAccount

``` diff
-namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount {
 {
-    public static class MicrosoftAccountDefaults {
 {
-        public const string AuthenticationScheme = "Microsoft";

-        public static readonly string AuthorizationEndpoint;

-        public static readonly string DisplayName;

-        public static readonly string TokenEndpoint;

-        public static readonly string UserInformationEndpoint;

-    }
-    public class MicrosoftAccountHandler : OAuthHandler<MicrosoftAccountOptions> {
 {
-        public MicrosoftAccountHandler(IOptionsMonitor<MicrosoftAccountOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);

-        protected override Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens);

-    }
-    public class MicrosoftAccountOptions : OAuthOptions {
 {
-        public MicrosoftAccountOptions();

-    }
-}
```

