# Microsoft.AspNetCore.Authentication.WsFederation

``` diff
-namespace Microsoft.AspNetCore.Authentication.WsFederation {
 {
-    public class AuthenticationFailedContext : RemoteAuthenticationContext<WsFederationOptions> {
 {
-        public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options);

-        public Exception Exception { get; set; }

-        public WsFederationMessage ProtocolMessage { get; set; }

-    }
-    public class MessageReceivedContext : RemoteAuthenticationContext<WsFederationOptions> {
 {
-        public MessageReceivedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, AuthenticationProperties properties);

-        public WsFederationMessage ProtocolMessage { get; set; }

-    }
-    public class RedirectContext : PropertiesContext<WsFederationOptions> {
 {
-        public RedirectContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, AuthenticationProperties properties);

-        public bool Handled { get; private set; }

-        public WsFederationMessage ProtocolMessage { get; set; }

-        public void HandleResponse();

-    }
-    public class RemoteSignOutContext : RemoteAuthenticationContext<WsFederationOptions> {
 {
-        public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, WsFederationMessage message);

-        public WsFederationMessage ProtocolMessage { get; set; }

-    }
-    public class SecurityTokenReceivedContext : RemoteAuthenticationContext<WsFederationOptions> {
 {
-        public SecurityTokenReceivedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, AuthenticationProperties properties);

-        public WsFederationMessage ProtocolMessage { get; set; }

-    }
-    public class SecurityTokenValidatedContext : RemoteAuthenticationContext<WsFederationOptions> {
 {
-        public SecurityTokenValidatedContext(HttpContext context, AuthenticationScheme scheme, WsFederationOptions options, ClaimsPrincipal principal, AuthenticationProperties properties);

-        public WsFederationMessage ProtocolMessage { get; set; }

-        public SecurityToken SecurityToken { get; set; }

-    }
-    public static class WsFederationDefaults {
 {
-        public const string AuthenticationScheme = "WsFederation";

-        public const string DisplayName = "WsFederation";

-        public static readonly string UserstatePropertiesKey;

-    }
-    public class WsFederationEvents : RemoteAuthenticationEvents {
 {
-        public WsFederationEvents();

-        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; }

-        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; }

-        public Func<RedirectContext, Task> OnRedirectToIdentityProvider { get; set; }

-        public Func<RemoteSignOutContext, Task> OnRemoteSignOut { get; set; }

-        public Func<SecurityTokenReceivedContext, Task> OnSecurityTokenReceived { get; set; }

-        public Func<SecurityTokenValidatedContext, Task> OnSecurityTokenValidated { get; set; }

-        public virtual Task AuthenticationFailed(AuthenticationFailedContext context);

-        public virtual Task MessageReceived(MessageReceivedContext context);

-        public virtual Task RedirectToIdentityProvider(RedirectContext context);

-        public virtual Task RemoteSignOut(RemoteSignOutContext context);

-        public virtual Task SecurityTokenReceived(SecurityTokenReceivedContext context);

-        public virtual Task SecurityTokenValidated(SecurityTokenValidatedContext context);

-    }
-    public class WsFederationHandler : RemoteAuthenticationHandler<WsFederationOptions>, IAuthenticationHandler, IAuthenticationSignOutHandler {
 {
-        public WsFederationHandler(IOptionsMonitor<WsFederationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);

-        protected new WsFederationEvents Events { get; set; }

-        protected override Task<object> CreateEventsAsync();

-        protected override Task HandleChallengeAsync(AuthenticationProperties properties);

-        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync();

-        protected virtual Task<bool> HandleRemoteSignOutAsync();

-        public override Task<bool> HandleRequestAsync();

-        public virtual Task SignOutAsync(AuthenticationProperties properties);

-    }
-    public class WsFederationOptions : RemoteAuthenticationOptions {
 {
-        public WsFederationOptions();

-        public bool AllowUnsolicitedLogins { get; set; }

-        public WsFederationConfiguration Configuration { get; set; }

-        public IConfigurationManager<WsFederationConfiguration> ConfigurationManager { get; set; }

-        public new WsFederationEvents Events { get; set; }

-        public string MetadataAddress { get; set; }

-        public bool RefreshOnIssuerKeyNotFound { get; set; }

-        public PathString RemoteSignOutPath { get; set; }

-        public bool RequireHttpsMetadata { get; set; }

-        public ICollection<ISecurityTokenValidator> SecurityTokenHandlers { get; set; }

-        public string SignOutScheme { get; set; }

-        public string SignOutWreply { get; set; }

-        public bool SkipUnrecognizedRequests { get; set; }

-        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

-        public TokenValidationParameters TokenValidationParameters { get; set; }

-        public bool UseTokenLifetime { get; set; }

-        public string Wreply { get; set; }

-        public string Wtrealm { get; set; }

-        public override void Validate();

-    }
-    public class WsFederationPostConfigureOptions : IPostConfigureOptions<WsFederationOptions> {
 {
-        public WsFederationPostConfigureOptions(IDataProtectionProvider dataProtection);

-        public void PostConfigure(string name, WsFederationOptions options);

-    }
-}
```

