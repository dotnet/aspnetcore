# Microsoft.AspNetCore.Authentication

``` diff
 namespace Microsoft.AspNetCore.Authentication {
+    public class AccessDeniedContext : HandleRequestContext<RemoteAuthenticationOptions> {
+        public AccessDeniedContext(HttpContext context, AuthenticationScheme scheme, RemoteAuthenticationOptions options);
+        public PathString AccessDeniedPath { get; set; }
+        public AuthenticationProperties Properties { get; set; }
+        public string ReturnUrl { get; set; }
+        public string ReturnUrlParameter { get; set; }
+    }
     public class AuthenticateResult {
         protected AuthenticateResult();
         public Exception Failure { get; protected set; }
         public bool None { get; protected set; }
         public ClaimsPrincipal Principal { get; }
         public AuthenticationProperties Properties { get; protected set; }
         public bool Succeeded { get; }
         public AuthenticationTicket Ticket { get; protected set; }
         public static AuthenticateResult Fail(Exception failure);
         public static AuthenticateResult Fail(Exception failure, AuthenticationProperties properties);
         public static AuthenticateResult Fail(string failureMessage);
         public static AuthenticateResult Fail(string failureMessage, AuthenticationProperties properties);
         public static AuthenticateResult NoResult();
         public static AuthenticateResult Success(AuthenticationTicket ticket);
     }
     public class AuthenticationBuilder {
         public AuthenticationBuilder(IServiceCollection services);
         public virtual IServiceCollection Services { get; }
         public virtual AuthenticationBuilder AddPolicyScheme(string authenticationScheme, string displayName, Action<PolicySchemeOptions> configureOptions);
         public virtual AuthenticationBuilder AddRemoteScheme<TOptions, THandler>(string authenticationScheme, string displayName, Action<TOptions> configureOptions) where TOptions : RemoteAuthenticationOptions, new() where THandler : RemoteAuthenticationHandler<TOptions>;
         public virtual AuthenticationBuilder AddScheme<TOptions, THandler>(string authenticationScheme, Action<TOptions> configureOptions) where TOptions : AuthenticationSchemeOptions, new() where THandler : AuthenticationHandler<TOptions>;
         public virtual AuthenticationBuilder AddScheme<TOptions, THandler>(string authenticationScheme, string displayName, Action<TOptions> configureOptions) where TOptions : AuthenticationSchemeOptions, new() where THandler : AuthenticationHandler<TOptions>;
     }
     public class AuthenticationFeature : IAuthenticationFeature {
         public AuthenticationFeature();
         public PathString OriginalPath { get; set; }
         public PathString OriginalPathBase { get; set; }
     }
     public abstract class AuthenticationHandler<TOptions> : IAuthenticationHandler where TOptions : AuthenticationSchemeOptions, new() {
         protected AuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);
         protected virtual string ClaimsIssuer { get; }
         protected ISystemClock Clock { get; }
         protected HttpContext Context { get; private set; }
         protected string CurrentUri { get; }
         protected virtual object Events { get; set; }
         protected ILogger Logger { get; }
         public TOptions Options { get; private set; }
         protected IOptionsMonitor<TOptions> OptionsMonitor { get; }
         protected PathString OriginalPath { get; }
         protected PathString OriginalPathBase { get; }
         protected HttpRequest Request { get; }
         protected HttpResponse Response { get; }
         public AuthenticationScheme Scheme { get; private set; }
         protected UrlEncoder UrlEncoder { get; }
         public Task<AuthenticateResult> AuthenticateAsync();
         protected string BuildRedirectUri(string targetPath);
         public Task ChallengeAsync(AuthenticationProperties properties);
         protected virtual Task<object> CreateEventsAsync();
         public Task ForbidAsync(AuthenticationProperties properties);
         protected abstract Task<AuthenticateResult> HandleAuthenticateAsync();
         protected Task<AuthenticateResult> HandleAuthenticateOnceAsync();
         protected Task<AuthenticateResult> HandleAuthenticateOnceSafeAsync();
         protected virtual Task HandleChallengeAsync(AuthenticationProperties properties);
         protected virtual Task HandleForbiddenAsync(AuthenticationProperties properties);
         public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context);
         protected virtual Task InitializeEventsAsync();
         protected virtual Task InitializeHandlerAsync();
         protected virtual string ResolveTarget(string scheme);
     }
     public class AuthenticationHandlerProvider : IAuthenticationHandlerProvider {
         public AuthenticationHandlerProvider(IAuthenticationSchemeProvider schemes);
         public IAuthenticationSchemeProvider Schemes { get; }
         public Task<IAuthenticationHandler> GetHandlerAsync(HttpContext context, string authenticationScheme);
     }
     public static class AuthenticationHttpContextExtensions {
         public static Task<AuthenticateResult> AuthenticateAsync(this HttpContext context);
         public static Task<AuthenticateResult> AuthenticateAsync(this HttpContext context, string scheme);
         public static Task ChallengeAsync(this HttpContext context);
         public static Task ChallengeAsync(this HttpContext context, AuthenticationProperties properties);
         public static Task ChallengeAsync(this HttpContext context, string scheme);
         public static Task ChallengeAsync(this HttpContext context, string scheme, AuthenticationProperties properties);
         public static Task ForbidAsync(this HttpContext context);
         public static Task ForbidAsync(this HttpContext context, AuthenticationProperties properties);
         public static Task ForbidAsync(this HttpContext context, string scheme);
         public static Task ForbidAsync(this HttpContext context, string scheme, AuthenticationProperties properties);
         public static Task<string> GetTokenAsync(this HttpContext context, string tokenName);
         public static Task<string> GetTokenAsync(this HttpContext context, string scheme, string tokenName);
         public static Task SignInAsync(this HttpContext context, ClaimsPrincipal principal);
         public static Task SignInAsync(this HttpContext context, ClaimsPrincipal principal, AuthenticationProperties properties);
         public static Task SignInAsync(this HttpContext context, string scheme, ClaimsPrincipal principal);
         public static Task SignInAsync(this HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties);
         public static Task SignOutAsync(this HttpContext context);
         public static Task SignOutAsync(this HttpContext context, AuthenticationProperties properties);
         public static Task SignOutAsync(this HttpContext context, string scheme);
         public static Task SignOutAsync(this HttpContext context, string scheme, AuthenticationProperties properties);
     }
     public class AuthenticationMiddleware {
         public AuthenticationMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes);
         public IAuthenticationSchemeProvider Schemes { get; set; }
         public Task Invoke(HttpContext context);
     }
     public class AuthenticationOptions {
         public AuthenticationOptions();
         public string DefaultAuthenticateScheme { get; set; }
         public string DefaultChallengeScheme { get; set; }
         public string DefaultForbidScheme { get; set; }
         public string DefaultScheme { get; set; }
         public string DefaultSignInScheme { get; set; }
         public string DefaultSignOutScheme { get; set; }
+        public bool RequireAuthenticatedSignIn { get; set; }
         public IDictionary<string, AuthenticationSchemeBuilder> SchemeMap { get; }
         public IEnumerable<AuthenticationSchemeBuilder> Schemes { get; }
         public void AddScheme(string name, Action<AuthenticationSchemeBuilder> configureBuilder);
         public void AddScheme<THandler>(string name, string displayName) where THandler : IAuthenticationHandler;
     }
     public class AuthenticationProperties {
         public AuthenticationProperties();
         public AuthenticationProperties(IDictionary<string, string> items);
         public AuthenticationProperties(IDictionary<string, string> items, IDictionary<string, object> parameters);
         public Nullable<bool> AllowRefresh { get; set; }
         public Nullable<DateTimeOffset> ExpiresUtc { get; set; }
         public bool IsPersistent { get; set; }
         public Nullable<DateTimeOffset> IssuedUtc { get; set; }
         public IDictionary<string, string> Items { get; }
         public IDictionary<string, object> Parameters { get; }
         public string RedirectUri { get; set; }
         protected Nullable<bool> GetBool(string key);
         protected Nullable<DateTimeOffset> GetDateTimeOffset(string key);
         public T GetParameter<T>(string key);
         public string GetString(string key);
         protected void SetBool(string key, Nullable<bool> value);
         protected void SetDateTimeOffset(string key, Nullable<DateTimeOffset> value);
         public void SetParameter<T>(string key, T value);
         public void SetString(string key, string value);
     }
     public class AuthenticationScheme {
         public AuthenticationScheme(string name, string displayName, Type handlerType);
         public string DisplayName { get; }
         public Type HandlerType { get; }
         public string Name { get; }
     }
     public class AuthenticationSchemeBuilder {
         public AuthenticationSchemeBuilder(string name);
         public string DisplayName { get; set; }
         public Type HandlerType { get; set; }
         public string Name { get; }
         public AuthenticationScheme Build();
     }
     public class AuthenticationSchemeOptions {
         public AuthenticationSchemeOptions();
         public string ClaimsIssuer { get; set; }
         public object Events { get; set; }
         public Type EventsType { get; set; }
         public string ForwardAuthenticate { get; set; }
         public string ForwardChallenge { get; set; }
         public string ForwardDefault { get; set; }
         public Func<HttpContext, string> ForwardDefaultSelector { get; set; }
         public string ForwardForbid { get; set; }
         public string ForwardSignIn { get; set; }
         public string ForwardSignOut { get; set; }
         public virtual void Validate();
         public virtual void Validate(string scheme);
     }
     public class AuthenticationSchemeProvider : IAuthenticationSchemeProvider {
         public AuthenticationSchemeProvider(IOptions<AuthenticationOptions> options);
         protected AuthenticationSchemeProvider(IOptions<AuthenticationOptions> options, IDictionary<string, AuthenticationScheme> schemes);
         public virtual void AddScheme(AuthenticationScheme scheme);
         public virtual Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync();
         public virtual Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync();
         public virtual Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync();
         public virtual Task<AuthenticationScheme> GetDefaultForbidSchemeAsync();
         public virtual Task<AuthenticationScheme> GetDefaultSignInSchemeAsync();
         public virtual Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync();
         public virtual Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync();
         public virtual Task<AuthenticationScheme> GetSchemeAsync(string name);
         public virtual void RemoveScheme(string name);
     }
     public class AuthenticationService : IAuthenticationService {
-        public AuthenticationService(IAuthenticationSchemeProvider schemes, IAuthenticationHandlerProvider handlers, IClaimsTransformation transform);

+        public AuthenticationService(IAuthenticationSchemeProvider schemes, IAuthenticationHandlerProvider handlers, IClaimsTransformation transform, IOptions<AuthenticationOptions> options);
         public IAuthenticationHandlerProvider Handlers { get; }
+        public AuthenticationOptions Options { get; }
         public IAuthenticationSchemeProvider Schemes { get; }
         public IClaimsTransformation Transform { get; }
         public virtual Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme);
         public virtual Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties);
         public virtual Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties);
         public virtual Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties);
         public virtual Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties);
     }
     public class AuthenticationTicket {
         public AuthenticationTicket(ClaimsPrincipal principal, AuthenticationProperties properties, string authenticationScheme);
         public AuthenticationTicket(ClaimsPrincipal principal, string authenticationScheme);
         public string AuthenticationScheme { get; private set; }
         public ClaimsPrincipal Principal { get; private set; }
         public AuthenticationProperties Properties { get; private set; }
     }
     public class AuthenticationToken {
         public AuthenticationToken();
         public string Name { get; set; }
         public string Value { get; set; }
     }
     public static class AuthenticationTokenExtensions {
         public static Task<string> GetTokenAsync(this IAuthenticationService auth, HttpContext context, string tokenName);
         public static Task<string> GetTokenAsync(this IAuthenticationService auth, HttpContext context, string scheme, string tokenName);
         public static IEnumerable<AuthenticationToken> GetTokens(this AuthenticationProperties properties);
         public static string GetTokenValue(this AuthenticationProperties properties, string tokenName);
         public static void StoreTokens(this AuthenticationProperties properties, IEnumerable<AuthenticationToken> tokens);
         public static bool UpdateTokenValue(this AuthenticationProperties properties, string tokenName, string tokenValue);
     }
     public static class Base64UrlTextEncoder {
         public static byte[] Decode(string text);
         public static string Encode(byte[] data);
     }
     public abstract class BaseContext<TOptions> where TOptions : AuthenticationSchemeOptions {
         protected BaseContext(HttpContext context, AuthenticationScheme scheme, TOptions options);
         public HttpContext HttpContext { get; }
         public TOptions Options { get; }
         public HttpRequest Request { get; }
         public HttpResponse Response { get; }
         public AuthenticationScheme Scheme { get; }
     }
     public static class ClaimActionCollectionMapExtensions {
         public static void DeleteClaim(this ClaimActionCollection collection, string claimType);
         public static void DeleteClaims(this ClaimActionCollection collection, params string[] claimTypes);
         public static void MapAll(this ClaimActionCollection collection);
         public static void MapAllExcept(this ClaimActionCollection collection, params string[] exclusions);
-        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, Func<JObject, string> resolver);

+        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, Func<JsonElement, string> resolver);
-        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, string valueType, Func<JObject, string> resolver);

+        public static void MapCustomJson(this ClaimActionCollection collection, string claimType, string valueType, Func<JsonElement, string> resolver);
         public static void MapJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey);
         public static void MapJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey, string valueType);
         public static void MapJsonSubKey(this ClaimActionCollection collection, string claimType, string jsonKey, string subKey);
         public static void MapJsonSubKey(this ClaimActionCollection collection, string claimType, string jsonKey, string subKey, string valueType);
     }
-    public static class ClaimActionCollectionUniqueExtensions {
 {
-        public static void MapUniqueJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey);

-        public static void MapUniqueJsonKey(this ClaimActionCollection collection, string claimType, string jsonKey, string valueType);

-    }
     public class HandleRequestContext<TOptions> : BaseContext<TOptions> where TOptions : AuthenticationSchemeOptions {
         protected HandleRequestContext(HttpContext context, AuthenticationScheme scheme, TOptions options);
         public HandleRequestResult Result { get; protected set; }
         public void HandleResponse();
         public void SkipHandler();
     }
     public class HandleRequestResult : AuthenticateResult {
         public HandleRequestResult();
         public bool Handled { get; private set; }
         public bool Skipped { get; private set; }
         public static new HandleRequestResult Fail(Exception failure);
         public static new HandleRequestResult Fail(Exception failure, AuthenticationProperties properties);
         public static new HandleRequestResult Fail(string failureMessage);
         public static new HandleRequestResult Fail(string failureMessage, AuthenticationProperties properties);
         public static HandleRequestResult Handle();
+        public static new HandleRequestResult NoResult();
         public static HandleRequestResult SkipHandler();
         public static new HandleRequestResult Success(AuthenticationTicket ticket);
     }
     public interface IAuthenticationFeature {
         PathString OriginalPath { get; set; }
         PathString OriginalPathBase { get; set; }
     }
     public interface IAuthenticationHandler {
         Task<AuthenticateResult> AuthenticateAsync();
         Task ChallengeAsync(AuthenticationProperties properties);
         Task ForbidAsync(AuthenticationProperties properties);
         Task InitializeAsync(AuthenticationScheme scheme, HttpContext context);
     }
     public interface IAuthenticationHandlerProvider {
         Task<IAuthenticationHandler> GetHandlerAsync(HttpContext context, string authenticationScheme);
     }
     public interface IAuthenticationRequestHandler : IAuthenticationHandler {
         Task<bool> HandleRequestAsync();
     }
     public interface IAuthenticationSchemeProvider {
         void AddScheme(AuthenticationScheme scheme);
         Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync();
         Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync();
         Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync();
         Task<AuthenticationScheme> GetDefaultForbidSchemeAsync();
         Task<AuthenticationScheme> GetDefaultSignInSchemeAsync();
         Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync();
         Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync();
         Task<AuthenticationScheme> GetSchemeAsync(string name);
         void RemoveScheme(string name);
     }
     public interface IAuthenticationService {
         Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme);
         Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties);
         Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties);
         Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties);
         Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties);
     }
     public interface IAuthenticationSignInHandler : IAuthenticationHandler, IAuthenticationSignOutHandler {
         Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties);
     }
     public interface IAuthenticationSignOutHandler : IAuthenticationHandler {
         Task SignOutAsync(AuthenticationProperties properties);
     }
     public interface IClaimsTransformation {
         Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal);
     }
     public interface IDataSerializer<TModel> {
         TModel Deserialize(byte[] data);
         byte[] Serialize(TModel model);
     }
     public interface ISecureDataFormat<TData> {
         string Protect(TData data);
         string Protect(TData data, string purpose);
         TData Unprotect(string protectedText);
         TData Unprotect(string protectedText, string purpose);
     }
     public interface ISystemClock {
         DateTimeOffset UtcNow { get; }
     }
+    public static class JsonDocumentAuthExtensions {
+        public static string GetString(this JsonElement element, string key);
+    }
     public class NoopClaimsTransformation : IClaimsTransformation {
         public NoopClaimsTransformation();
         public virtual Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal);
     }
     public class PolicySchemeHandler : SignInAuthenticationHandler<PolicySchemeOptions> {
         public PolicySchemeHandler(IOptionsMonitor<PolicySchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);
         protected override Task<AuthenticateResult> HandleAuthenticateAsync();
         protected override Task HandleChallengeAsync(AuthenticationProperties properties);
         protected override Task HandleForbiddenAsync(AuthenticationProperties properties);
         protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties);
         protected override Task HandleSignOutAsync(AuthenticationProperties properties);
     }
     public class PolicySchemeOptions : AuthenticationSchemeOptions {
         public PolicySchemeOptions();
     }
     public abstract class PrincipalContext<TOptions> : PropertiesContext<TOptions> where TOptions : AuthenticationSchemeOptions {
         protected PrincipalContext(HttpContext context, AuthenticationScheme scheme, TOptions options, AuthenticationProperties properties);
         public virtual ClaimsPrincipal Principal { get; set; }
     }
     public abstract class PropertiesContext<TOptions> : BaseContext<TOptions> where TOptions : AuthenticationSchemeOptions {
         protected PropertiesContext(HttpContext context, AuthenticationScheme scheme, TOptions options, AuthenticationProperties properties);
         public virtual AuthenticationProperties Properties { get; protected set; }
     }
     public class PropertiesDataFormat : SecureDataFormat<AuthenticationProperties> {
         public PropertiesDataFormat(IDataProtector protector);
     }
     public class PropertiesSerializer : IDataSerializer<AuthenticationProperties> {
         public PropertiesSerializer();
         public static PropertiesSerializer Default { get; }
         public virtual AuthenticationProperties Deserialize(byte[] data);
         public virtual AuthenticationProperties Read(BinaryReader reader);
         public virtual byte[] Serialize(AuthenticationProperties model);
         public virtual void Write(BinaryWriter writer, AuthenticationProperties properties);
     }
     public class RedirectContext<TOptions> : PropertiesContext<TOptions> where TOptions : AuthenticationSchemeOptions {
         public RedirectContext(HttpContext context, AuthenticationScheme scheme, TOptions options, AuthenticationProperties properties, string redirectUri);
         public string RedirectUri { get; set; }
     }
     public abstract class RemoteAuthenticationContext<TOptions> : HandleRequestContext<TOptions> where TOptions : AuthenticationSchemeOptions {
         protected RemoteAuthenticationContext(HttpContext context, AuthenticationScheme scheme, TOptions options, AuthenticationProperties properties);
         public ClaimsPrincipal Principal { get; set; }
         public virtual AuthenticationProperties Properties { get; set; }
         public void Fail(Exception failure);
         public void Fail(string failureMessage);
         public void Success();
     }
     public class RemoteAuthenticationEvents {
         public RemoteAuthenticationEvents();
+        public Func<AccessDeniedContext, Task> OnAccessDenied { get; set; }
         public Func<RemoteFailureContext, Task> OnRemoteFailure { get; set; }
         public Func<TicketReceivedContext, Task> OnTicketReceived { get; set; }
+        public virtual Task AccessDenied(AccessDeniedContext context);
         public virtual Task RemoteFailure(RemoteFailureContext context);
         public virtual Task TicketReceived(TicketReceivedContext context);
     }
     public abstract class RemoteAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationHandler, IAuthenticationRequestHandler where TOptions : RemoteAuthenticationOptions, new() {
         protected RemoteAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);
         protected new RemoteAuthenticationEvents Events { get; set; }
         protected string SignInScheme { get; }
         protected override Task<object> CreateEventsAsync();
         protected virtual void GenerateCorrelationId(AuthenticationProperties properties);
+        protected virtual Task<HandleRequestResult> HandleAccessDeniedErrorAsync(AuthenticationProperties properties);
         protected override Task<AuthenticateResult> HandleAuthenticateAsync();
         protected override Task HandleForbiddenAsync(AuthenticationProperties properties);
         protected abstract Task<HandleRequestResult> HandleRemoteAuthenticateAsync();
         public virtual Task<bool> HandleRequestAsync();
         public virtual Task<bool> ShouldHandleRequestAsync();
         protected virtual bool ValidateCorrelationId(AuthenticationProperties properties);
     }
     public class RemoteAuthenticationOptions : AuthenticationSchemeOptions {
         public RemoteAuthenticationOptions();
+        public PathString AccessDeniedPath { get; set; }
         public HttpClient Backchannel { get; set; }
         public HttpMessageHandler BackchannelHttpHandler { get; set; }
         public TimeSpan BackchannelTimeout { get; set; }
         public PathString CallbackPath { get; set; }
         public CookieBuilder CorrelationCookie { get; set; }
         public IDataProtectionProvider DataProtectionProvider { get; set; }
         public new RemoteAuthenticationEvents Events { get; set; }
         public TimeSpan RemoteAuthenticationTimeout { get; set; }
+        public string ReturnUrlParameter { get; set; }
         public bool SaveTokens { get; set; }
         public string SignInScheme { get; set; }
         public override void Validate();
         public override void Validate(string scheme);
     }
     public class RemoteFailureContext : HandleRequestContext<RemoteAuthenticationOptions> {
         public RemoteFailureContext(HttpContext context, AuthenticationScheme scheme, RemoteAuthenticationOptions options, Exception failure);
         public Exception Failure { get; set; }
         public AuthenticationProperties Properties { get; set; }
     }
     public abstract class ResultContext<TOptions> : BaseContext<TOptions> where TOptions : AuthenticationSchemeOptions {
         protected ResultContext(HttpContext context, AuthenticationScheme scheme, TOptions options);
         public ClaimsPrincipal Principal { get; set; }
         public AuthenticationProperties Properties { get; set; }
         public AuthenticateResult Result { get; private set; }
         public void Fail(Exception failure);
         public void Fail(string failureMessage);
         public void NoResult();
         public void Success();
     }
     public class SecureDataFormat<TData> : ISecureDataFormat<TData> {
         public SecureDataFormat(IDataSerializer<TData> serializer, IDataProtector protector);
         public string Protect(TData data);
         public string Protect(TData data, string purpose);
         public TData Unprotect(string protectedText);
         public TData Unprotect(string protectedText, string purpose);
     }
     public abstract class SignInAuthenticationHandler<TOptions> : SignOutAuthenticationHandler<TOptions>, IAuthenticationHandler, IAuthenticationSignInHandler, IAuthenticationSignOutHandler where TOptions : AuthenticationSchemeOptions, new() {
         public SignInAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);
         protected abstract Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties);
         public virtual Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties);
     }
     public abstract class SignOutAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationHandler, IAuthenticationSignOutHandler where TOptions : AuthenticationSchemeOptions, new() {
         public SignOutAuthenticationHandler(IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);
         protected abstract Task HandleSignOutAsync(AuthenticationProperties properties);
         public virtual Task SignOutAsync(AuthenticationProperties properties);
     }
     public class SystemClock : ISystemClock {
         public SystemClock();
         public DateTimeOffset UtcNow { get; }
     }
     public class TicketDataFormat : SecureDataFormat<AuthenticationTicket> {
         public TicketDataFormat(IDataProtector protector);
     }
     public class TicketReceivedContext : RemoteAuthenticationContext<RemoteAuthenticationOptions> {
         public TicketReceivedContext(HttpContext context, AuthenticationScheme scheme, RemoteAuthenticationOptions options, AuthenticationTicket ticket);
         public string ReturnUri { get; set; }
     }
     public class TicketSerializer : IDataSerializer<AuthenticationTicket> {
         public TicketSerializer();
         public static TicketSerializer Default { get; }
         public virtual AuthenticationTicket Deserialize(byte[] data);
         public virtual AuthenticationTicket Read(BinaryReader reader);
         protected virtual Claim ReadClaim(BinaryReader reader, ClaimsIdentity identity);
         protected virtual ClaimsIdentity ReadIdentity(BinaryReader reader);
         public virtual byte[] Serialize(AuthenticationTicket ticket);
         public virtual void Write(BinaryWriter writer, AuthenticationTicket ticket);
         protected virtual void WriteClaim(BinaryWriter writer, Claim claim);
         protected virtual void WriteIdentity(BinaryWriter writer, ClaimsIdentity identity);
     }
 }
```

