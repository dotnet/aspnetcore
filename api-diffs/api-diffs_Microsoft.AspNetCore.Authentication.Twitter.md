# Microsoft.AspNetCore.Authentication.Twitter

``` diff
-namespace Microsoft.AspNetCore.Authentication.Twitter {
 {
-    public class AccessToken : RequestToken {
 {
-        public AccessToken();

-        public string ScreenName { get; set; }

-        public string UserId { get; set; }

-    }
-    public class RequestToken {
 {
-        public RequestToken();

-        public bool CallbackConfirmed { get; set; }

-        public AuthenticationProperties Properties { get; set; }

-        public string Token { get; set; }

-        public string TokenSecret { get; set; }

-    }
-    public class RequestTokenSerializer : IDataSerializer<RequestToken> {
 {
-        public RequestTokenSerializer();

-        public virtual RequestToken Deserialize(byte[] data);

-        public static RequestToken Read(BinaryReader reader);

-        public virtual byte[] Serialize(RequestToken model);

-        public static void Write(BinaryWriter writer, RequestToken token);

-    }
-    public class TwitterCreatingTicketContext : ResultContext<TwitterOptions> {
 {
-        public TwitterCreatingTicketContext(HttpContext context, AuthenticationScheme scheme, TwitterOptions options, ClaimsPrincipal principal, AuthenticationProperties properties, string userId, string screenName, string accessToken, string accessTokenSecret, JObject user);

-        public string AccessToken { get; }

-        public string AccessTokenSecret { get; }

-        public string ScreenName { get; }

-        public JObject User { get; }

-        public string UserId { get; }

-    }
-    public static class TwitterDefaults {
 {
-        public const string AuthenticationScheme = "Twitter";

-        public static readonly string DisplayName;

-    }
-    public class TwitterEvents : RemoteAuthenticationEvents {
 {
-        public TwitterEvents();

-        public Func<TwitterCreatingTicketContext, Task> OnCreatingTicket { get; set; }

-        public Func<RedirectContext<TwitterOptions>, Task> OnRedirectToAuthorizationEndpoint { get; set; }

-        public virtual Task CreatingTicket(TwitterCreatingTicketContext context);

-        public virtual Task RedirectToAuthorizationEndpoint(RedirectContext<TwitterOptions> context);

-    }
-    public class TwitterHandler : RemoteAuthenticationHandler<TwitterOptions> {
 {
-        public TwitterHandler(IOptionsMonitor<TwitterOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);

-        protected new TwitterEvents Events { get; set; }

-        protected override Task<object> CreateEventsAsync();

-        protected virtual Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, AccessToken token, JObject user);

-        protected override Task HandleChallengeAsync(AuthenticationProperties properties);

-        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync();

-    }
-    public class TwitterOptions : RemoteAuthenticationOptions {
 {
-        public TwitterOptions();

-        public ClaimActionCollection ClaimActions { get; }

-        public string ConsumerKey { get; set; }

-        public string ConsumerSecret { get; set; }

-        public new TwitterEvents Events { get; set; }

-        public bool RetrieveUserDetails { get; set; }

-        public CookieBuilder StateCookie { get; set; }

-        public ISecureDataFormat<RequestToken> StateDataFormat { get; set; }

-        public override void Validate();

-    }
-    public class TwitterPostConfigureOptions : IPostConfigureOptions<TwitterOptions> {
 {
-        public TwitterPostConfigureOptions(IDataProtectionProvider dataProtection);

-        public void PostConfigure(string name, TwitterOptions options);

-    }
-}
```

