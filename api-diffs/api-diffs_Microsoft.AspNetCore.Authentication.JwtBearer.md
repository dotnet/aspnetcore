# Microsoft.AspNetCore.Authentication.JwtBearer

``` diff
-namespace Microsoft.AspNetCore.Authentication.JwtBearer {
 {
-    public class AuthenticationFailedContext : ResultContext<JwtBearerOptions> {
 {
-        public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options);

-        public Exception Exception { get; set; }

-    }
-    public class JwtBearerChallengeContext : PropertiesContext<JwtBearerOptions> {
 {
-        public JwtBearerChallengeContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options, AuthenticationProperties properties);

-        public Exception AuthenticateFailure { get; set; }

-        public string Error { get; set; }

-        public string ErrorDescription { get; set; }

-        public string ErrorUri { get; set; }

-        public bool Handled { get; private set; }

-        public void HandleResponse();

-    }
-    public static class JwtBearerDefaults {
 {
-        public const string AuthenticationScheme = "Bearer";

-    }
-    public class JwtBearerEvents {
 {
-        public JwtBearerEvents();

-        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; }

-        public Func<JwtBearerChallengeContext, Task> OnChallenge { get; set; }

-        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; }

-        public Func<TokenValidatedContext, Task> OnTokenValidated { get; set; }

-        public virtual Task AuthenticationFailed(AuthenticationFailedContext context);

-        public virtual Task Challenge(JwtBearerChallengeContext context);

-        public virtual Task MessageReceived(MessageReceivedContext context);

-        public virtual Task TokenValidated(TokenValidatedContext context);

-    }
-    public class JwtBearerHandler : AuthenticationHandler<JwtBearerOptions> {
 {
-        public JwtBearerHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, IDataProtectionProvider dataProtection, ISystemClock clock);

-        protected new JwtBearerEvents Events { get; set; }

-        protected override Task<object> CreateEventsAsync();

-        protected override Task<AuthenticateResult> HandleAuthenticateAsync();

-        protected override Task HandleChallengeAsync(AuthenticationProperties properties);

-    }
-    public class JwtBearerOptions : AuthenticationSchemeOptions {
 {
-        public JwtBearerOptions();

-        public string Audience { get; set; }

-        public string Authority { get; set; }

-        public HttpMessageHandler BackchannelHttpHandler { get; set; }

-        public TimeSpan BackchannelTimeout { get; set; }

-        public string Challenge { get; set; }

-        public OpenIdConnectConfiguration Configuration { get; set; }

-        public IConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager { get; set; }

-        public new JwtBearerEvents Events { get; set; }

-        public bool IncludeErrorDetails { get; set; }

-        public string MetadataAddress { get; set; }

-        public bool RefreshOnIssuerKeyNotFound { get; set; }

-        public bool RequireHttpsMetadata { get; set; }

-        public bool SaveToken { get; set; }

-        public IList<ISecurityTokenValidator> SecurityTokenValidators { get; }

-        public TokenValidationParameters TokenValidationParameters { get; set; }

-    }
-    public class JwtBearerPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions> {
 {
-        public JwtBearerPostConfigureOptions();

-        public void PostConfigure(string name, JwtBearerOptions options);

-    }
-    public class MessageReceivedContext : ResultContext<JwtBearerOptions> {
 {
-        public MessageReceivedContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options);

-        public string Token { get; set; }

-    }
-    public class TokenValidatedContext : ResultContext<JwtBearerOptions> {
 {
-        public TokenValidatedContext(HttpContext context, AuthenticationScheme scheme, JwtBearerOptions options);

-        public SecurityToken SecurityToken { get; set; }

-    }
-}
```

