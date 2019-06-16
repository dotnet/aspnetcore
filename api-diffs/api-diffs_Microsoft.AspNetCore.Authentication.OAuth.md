# Microsoft.AspNetCore.Authentication.OAuth

``` diff
 namespace Microsoft.AspNetCore.Authentication.OAuth {
     public class OAuthChallengeProperties : AuthenticationProperties {
         public static readonly string ScopeKey;
         public OAuthChallengeProperties();
         public OAuthChallengeProperties(IDictionary<string, string> items);
         public OAuthChallengeProperties(IDictionary<string, string> items, IDictionary<string, object> parameters);
         public ICollection<string> Scope { get; set; }
         public virtual void SetScope(params string[] scopes);
     }
+    public class OAuthCodeExchangeContext {
+        public OAuthCodeExchangeContext(AuthenticationProperties properties, string code, string redirectUri);
+        public string Code { get; }
+        public AuthenticationProperties Properties { get; }
+        public string RedirectUri { get; }
+    }
+    public static class OAuthConstants {
+        public static readonly string CodeChallengeKey;
+        public static readonly string CodeChallengeMethodKey;
+        public static readonly string CodeChallengeMethodS256;
+        public static readonly string CodeVerifierKey;
+    }
     public class OAuthCreatingTicketContext : ResultContext<OAuthOptions> {
-        public OAuthCreatingTicketContext(ClaimsPrincipal principal, AuthenticationProperties properties, HttpContext context, AuthenticationScheme scheme, OAuthOptions options, HttpClient backchannel, OAuthTokenResponse tokens);

-        public OAuthCreatingTicketContext(ClaimsPrincipal principal, AuthenticationProperties properties, HttpContext context, AuthenticationScheme scheme, OAuthOptions options, HttpClient backchannel, OAuthTokenResponse tokens, JObject user);

+        public OAuthCreatingTicketContext(ClaimsPrincipal principal, AuthenticationProperties properties, HttpContext context, AuthenticationScheme scheme, OAuthOptions options, HttpClient backchannel, OAuthTokenResponse tokens, JsonElement user);
         public string AccessToken { get; }
         public HttpClient Backchannel { get; }
         public Nullable<TimeSpan> ExpiresIn { get; }
         public ClaimsIdentity Identity { get; }
         public string RefreshToken { get; }
         public OAuthTokenResponse TokenResponse { get; }
         public string TokenType { get; }
-        public JObject User { get; }
+        public JsonElement User { get; }
         public void RunClaimActions();
-        public void RunClaimActions(JObject userData);

+        public void RunClaimActions(JsonElement userData);
     }
     public static class OAuthDefaults {
         public static readonly string DisplayName;
     }
     public class OAuthEvents : RemoteAuthenticationEvents {
         public OAuthEvents();
         public Func<OAuthCreatingTicketContext, Task> OnCreatingTicket { get; set; }
         public Func<RedirectContext<OAuthOptions>, Task> OnRedirectToAuthorizationEndpoint { get; set; }
         public virtual Task CreatingTicket(OAuthCreatingTicketContext context);
         public virtual Task RedirectToAuthorizationEndpoint(RedirectContext<OAuthOptions> context);
     }
     public class OAuthHandler<TOptions> : RemoteAuthenticationHandler<TOptions> where TOptions : OAuthOptions, new() {
         public OAuthHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);
         protected HttpClient Backchannel { get; }
         protected new OAuthEvents Events { get; set; }
         protected virtual string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri);
         protected override Task<object> CreateEventsAsync();
         protected virtual Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens);
+        protected virtual Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context);
-        protected virtual Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri);

         protected virtual string FormatScope();
         protected virtual string FormatScope(IEnumerable<string> scopes);
         protected override Task HandleChallengeAsync(AuthenticationProperties properties);
         protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync();
     }
     public class OAuthOptions : RemoteAuthenticationOptions {
         public OAuthOptions();
         public string AuthorizationEndpoint { get; set; }
         public ClaimActionCollection ClaimActions { get; }
         public string ClientId { get; set; }
         public string ClientSecret { get; set; }
         public new OAuthEvents Events { get; set; }
         public ICollection<string> Scope { get; }
         public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }
         public string TokenEndpoint { get; set; }
+        public bool UsePkce { get; set; }
         public string UserInformationEndpoint { get; set; }
         public override void Validate();
     }
-    public class OAuthTokenResponse {
+    public class OAuthTokenResponse : IDisposable {
         public string AccessToken { get; set; }
         public Exception Error { get; set; }
         public string ExpiresIn { get; set; }
         public string RefreshToken { get; set; }
-        public JObject Response { get; set; }
+        public JsonDocument Response { get; set; }
         public string TokenType { get; set; }
+        public void Dispose();
         public static OAuthTokenResponse Failed(Exception error);
-        public static OAuthTokenResponse Success(JObject response);

+        public static OAuthTokenResponse Success(JsonDocument response);
     }
 }
```

