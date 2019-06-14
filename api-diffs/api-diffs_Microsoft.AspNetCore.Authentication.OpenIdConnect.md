# Microsoft.AspNetCore.Authentication.OpenIdConnect

``` diff
-namespace Microsoft.AspNetCore.Authentication.OpenIdConnect {
 {
-    public class AuthenticationFailedContext : RemoteAuthenticationContext<OpenIdConnectOptions> {
 {
-        public AuthenticationFailedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options);

-        public Exception Exception { get; set; }

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-    }
-    public class AuthorizationCodeReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions> {
 {
-        public AuthorizationCodeReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, AuthenticationProperties properties);

-        public HttpClient Backchannel { get; internal set; }

-        public bool HandledCodeRedemption { get; }

-        public JwtSecurityToken JwtSecurityToken { get; set; }

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-        public OpenIdConnectMessage TokenEndpointRequest { get; set; }

-        public OpenIdConnectMessage TokenEndpointResponse { get; set; }

-        public void HandleCodeRedemption();

-        public void HandleCodeRedemption(OpenIdConnectMessage tokenEndpointResponse);

-        public void HandleCodeRedemption(string accessToken, string idToken);

-    }
-    public class MessageReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions> {
 {
-        public MessageReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, AuthenticationProperties properties);

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-        public string Token { get; set; }

-    }
-    public class OpenIdConnectChallengeProperties : OAuthChallengeProperties {
 {
-        public static readonly string MaxAgeKey;

-        public static readonly string PromptKey;

-        public OpenIdConnectChallengeProperties();

-        public OpenIdConnectChallengeProperties(IDictionary<string, string> items);

-        public OpenIdConnectChallengeProperties(IDictionary<string, string> items, IDictionary<string, object> parameters);

-        public Nullable<TimeSpan> MaxAge { get; set; }

-        public string Prompt { get; set; }

-    }
-    public static class OpenIdConnectDefaults {
 {
-        public static readonly string AuthenticationPropertiesKey;

-        public const string AuthenticationScheme = "OpenIdConnect";

-        public static readonly string CookieNoncePrefix;

-        public static readonly string DisplayName;

-        public static readonly string RedirectUriForCodePropertiesKey;

-        public static readonly string UserstatePropertiesKey;

-    }
-    public class OpenIdConnectEvents : RemoteAuthenticationEvents {
 {
-        public OpenIdConnectEvents();

-        public Func<AuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; }

-        public Func<AuthorizationCodeReceivedContext, Task> OnAuthorizationCodeReceived { get; set; }

-        public Func<MessageReceivedContext, Task> OnMessageReceived { get; set; }

-        public Func<RedirectContext, Task> OnRedirectToIdentityProvider { get; set; }

-        public Func<RedirectContext, Task> OnRedirectToIdentityProviderForSignOut { get; set; }

-        public Func<RemoteSignOutContext, Task> OnRemoteSignOut { get; set; }

-        public Func<RemoteSignOutContext, Task> OnSignedOutCallbackRedirect { get; set; }

-        public Func<TokenResponseReceivedContext, Task> OnTokenResponseReceived { get; set; }

-        public Func<TokenValidatedContext, Task> OnTokenValidated { get; set; }

-        public Func<UserInformationReceivedContext, Task> OnUserInformationReceived { get; set; }

-        public virtual Task AuthenticationFailed(AuthenticationFailedContext context);

-        public virtual Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context);

-        public virtual Task MessageReceived(MessageReceivedContext context);

-        public virtual Task RedirectToIdentityProvider(RedirectContext context);

-        public virtual Task RedirectToIdentityProviderForSignOut(RedirectContext context);

-        public virtual Task RemoteSignOut(RemoteSignOutContext context);

-        public virtual Task SignedOutCallbackRedirect(RemoteSignOutContext context);

-        public virtual Task TokenResponseReceived(TokenResponseReceivedContext context);

-        public virtual Task TokenValidated(TokenValidatedContext context);

-        public virtual Task UserInformationReceived(UserInformationReceivedContext context);

-    }
-    public class OpenIdConnectHandler : RemoteAuthenticationHandler<OpenIdConnectOptions>, IAuthenticationHandler, IAuthenticationSignOutHandler {
 {
-        public OpenIdConnectHandler(IOptionsMonitor<OpenIdConnectOptions> options, ILoggerFactory logger, HtmlEncoder htmlEncoder, UrlEncoder encoder, ISystemClock clock);

-        protected HttpClient Backchannel { get; }

-        protected new OpenIdConnectEvents Events { get; set; }

-        protected HtmlEncoder HtmlEncoder { get; }

-        protected override Task<object> CreateEventsAsync();

-        protected virtual Task<HandleRequestResult> GetUserInformationAsync(OpenIdConnectMessage message, JwtSecurityToken jwt, ClaimsPrincipal principal, AuthenticationProperties properties);

-        protected override Task HandleChallengeAsync(AuthenticationProperties properties);

-        protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync();

-        protected virtual Task<bool> HandleRemoteSignOutAsync();

-        public override Task<bool> HandleRequestAsync();

-        protected virtual Task<bool> HandleSignOutCallbackAsync();

-        protected virtual Task<OpenIdConnectMessage> RedeemAuthorizationCodeAsync(OpenIdConnectMessage tokenEndpointRequest);

-        public virtual Task SignOutAsync(AuthenticationProperties properties);

-    }
-    public class OpenIdConnectOptions : RemoteAuthenticationOptions {
 {
-        public OpenIdConnectOptions();

-        public OpenIdConnectRedirectBehavior AuthenticationMethod { get; set; }

-        public string Authority { get; set; }

-        public ClaimActionCollection ClaimActions { get; }

-        public string ClientId { get; set; }

-        public string ClientSecret { get; set; }

-        public OpenIdConnectConfiguration Configuration { get; set; }

-        public IConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager { get; set; }

-        public bool DisableTelemetry { get; set; }

-        public new OpenIdConnectEvents Events { get; set; }

-        public bool GetClaimsFromUserInfoEndpoint { get; set; }

-        public Nullable<TimeSpan> MaxAge { get; set; }

-        public string MetadataAddress { get; set; }

-        public CookieBuilder NonceCookie { get; set; }

-        public string Prompt { get; set; }

-        public OpenIdConnectProtocolValidator ProtocolValidator { get; set; }

-        public bool RefreshOnIssuerKeyNotFound { get; set; }

-        public PathString RemoteSignOutPath { get; set; }

-        public bool RequireHttpsMetadata { get; set; }

-        public string Resource { get; set; }

-        public string ResponseMode { get; set; }

-        public string ResponseType { get; set; }

-        public ICollection<string> Scope { get; }

-        public ISecurityTokenValidator SecurityTokenValidator { get; set; }

-        public PathString SignedOutCallbackPath { get; set; }

-        public string SignedOutRedirectUri { get; set; }

-        public string SignOutScheme { get; set; }

-        public bool SkipUnrecognizedRequests { get; set; }

-        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }

-        public ISecureDataFormat<string> StringDataFormat { get; set; }

-        public TokenValidationParameters TokenValidationParameters { get; set; }

-        public bool UseTokenLifetime { get; set; }

-        public override void Validate();

-    }
-    public class OpenIdConnectPostConfigureOptions : IPostConfigureOptions<OpenIdConnectOptions> {
 {
-        public OpenIdConnectPostConfigureOptions(IDataProtectionProvider dataProtection);

-        public void PostConfigure(string name, OpenIdConnectOptions options);

-    }
-    public enum OpenIdConnectRedirectBehavior {
 {
-        FormPost = 1,

-        RedirectGet = 0,

-    }
-    public class RedirectContext : PropertiesContext<OpenIdConnectOptions> {
 {
-        public RedirectContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, AuthenticationProperties properties);

-        public bool Handled { get; private set; }

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-        public void HandleResponse();

-    }
-    public class RemoteSignOutContext : RemoteAuthenticationContext<OpenIdConnectOptions> {
 {
-        public RemoteSignOutContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, OpenIdConnectMessage message);

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-    }
-    public class TokenResponseReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions> {
 {
-        public TokenResponseReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal user, AuthenticationProperties properties);

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-        public OpenIdConnectMessage TokenEndpointResponse { get; set; }

-    }
-    public class TokenValidatedContext : RemoteAuthenticationContext<OpenIdConnectOptions> {
 {
-        public TokenValidatedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal principal, AuthenticationProperties properties);

-        public string Nonce { get; set; }

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-        public JwtSecurityToken SecurityToken { get; set; }

-        public OpenIdConnectMessage TokenEndpointResponse { get; set; }

-    }
-    public class UserInformationReceivedContext : RemoteAuthenticationContext<OpenIdConnectOptions> {
 {
-        public UserInformationReceivedContext(HttpContext context, AuthenticationScheme scheme, OpenIdConnectOptions options, ClaimsPrincipal principal, AuthenticationProperties properties);

-        public OpenIdConnectMessage ProtocolMessage { get; set; }

-        public JObject User { get; set; }

-    }
-}
```

