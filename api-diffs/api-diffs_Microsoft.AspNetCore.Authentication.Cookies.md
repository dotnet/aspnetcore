# Microsoft.AspNetCore.Authentication.Cookies

``` diff
 namespace Microsoft.AspNetCore.Authentication.Cookies {
     public class ChunkingCookieManager : ICookieManager {
         public const int DefaultChunkSize = 4050;
         public ChunkingCookieManager();
         public Nullable<int> ChunkSize { get; set; }
         public bool ThrowForPartialCookies { get; set; }
         public void AppendResponseCookie(HttpContext context, string key, string value, CookieOptions options);
         public void DeleteCookie(HttpContext context, string key, CookieOptions options);
         public string GetRequestCookie(HttpContext context, string key);
     }
     public static class CookieAuthenticationDefaults {
         public static readonly PathString AccessDeniedPath;
         public static readonly PathString LoginPath;
         public static readonly PathString LogoutPath;
         public const string AuthenticationScheme = "Cookies";
         public static readonly string CookiePrefix;
         public static readonly string ReturnUrlParameter;
     }
     public class CookieAuthenticationEvents {
         public CookieAuthenticationEvents();
         public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToAccessDenied { get; set; }
         public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToLogin { get; set; }
         public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToLogout { get; set; }
         public Func<RedirectContext<CookieAuthenticationOptions>, Task> OnRedirectToReturnUrl { get; set; }
         public Func<CookieSignedInContext, Task> OnSignedIn { get; set; }
         public Func<CookieSigningInContext, Task> OnSigningIn { get; set; }
         public Func<CookieSigningOutContext, Task> OnSigningOut { get; set; }
         public Func<CookieValidatePrincipalContext, Task> OnValidatePrincipal { get; set; }
         public virtual Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context);
         public virtual Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context);
         public virtual Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context);
         public virtual Task RedirectToReturnUrl(RedirectContext<CookieAuthenticationOptions> context);
         public virtual Task SignedIn(CookieSignedInContext context);
         public virtual Task SigningIn(CookieSigningInContext context);
         public virtual Task SigningOut(CookieSigningOutContext context);
         public virtual Task ValidatePrincipal(CookieValidatePrincipalContext context);
     }
     public class CookieAuthenticationHandler : SignInAuthenticationHandler<CookieAuthenticationOptions> {
         public CookieAuthenticationHandler(IOptionsMonitor<CookieAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock);
         protected new CookieAuthenticationEvents Events { get; set; }
         protected override Task<object> CreateEventsAsync();
         protected virtual Task FinishResponseAsync();
         protected override Task<AuthenticateResult> HandleAuthenticateAsync();
         protected override Task HandleChallengeAsync(AuthenticationProperties properties);
         protected override Task HandleForbiddenAsync(AuthenticationProperties properties);
         protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties);
         protected override Task HandleSignOutAsync(AuthenticationProperties properties);
         protected override Task InitializeHandlerAsync();
     }
     public class CookieAuthenticationOptions : AuthenticationSchemeOptions {
         public CookieAuthenticationOptions();
         public PathString AccessDeniedPath { get; set; }
         public CookieBuilder Cookie { get; set; }
-        public string CookieDomain { get; set; }

-        public bool CookieHttpOnly { get; set; }

         public ICookieManager CookieManager { get; set; }
-        public string CookieName { get; set; }

-        public string CookiePath { get; set; }

-        public CookieSecurePolicy CookieSecure { get; set; }

         public IDataProtectionProvider DataProtectionProvider { get; set; }
         public new CookieAuthenticationEvents Events { get; set; }
         public TimeSpan ExpireTimeSpan { get; set; }
         public PathString LoginPath { get; set; }
         public PathString LogoutPath { get; set; }
         public string ReturnUrlParameter { get; set; }
         public ITicketStore SessionStore { get; set; }
         public bool SlidingExpiration { get; set; }
         public ISecureDataFormat<AuthenticationTicket> TicketDataFormat { get; set; }
     }
     public class CookieSignedInContext : PrincipalContext<CookieAuthenticationOptions> {
         public CookieSignedInContext(HttpContext context, AuthenticationScheme scheme, ClaimsPrincipal principal, AuthenticationProperties properties, CookieAuthenticationOptions options);
     }
     public class CookieSigningInContext : PrincipalContext<CookieAuthenticationOptions> {
         public CookieSigningInContext(HttpContext context, AuthenticationScheme scheme, CookieAuthenticationOptions options, ClaimsPrincipal principal, AuthenticationProperties properties, CookieOptions cookieOptions);
         public CookieOptions CookieOptions { get; set; }
     }
     public class CookieSigningOutContext : PropertiesContext<CookieAuthenticationOptions> {
         public CookieSigningOutContext(HttpContext context, AuthenticationScheme scheme, CookieAuthenticationOptions options, AuthenticationProperties properties, CookieOptions cookieOptions);
         public CookieOptions CookieOptions { get; set; }
     }
     public class CookieValidatePrincipalContext : PrincipalContext<CookieAuthenticationOptions> {
         public CookieValidatePrincipalContext(HttpContext context, AuthenticationScheme scheme, CookieAuthenticationOptions options, AuthenticationTicket ticket);
         public bool ShouldRenew { get; set; }
         public void RejectPrincipal();
         public void ReplacePrincipal(ClaimsPrincipal principal);
     }
     public interface ICookieManager {
         void AppendResponseCookie(HttpContext context, string key, string value, CookieOptions options);
         void DeleteCookie(HttpContext context, string key, CookieOptions options);
         string GetRequestCookie(HttpContext context, string key);
     }
     public interface ITicketStore {
         Task RemoveAsync(string key);
         Task RenewAsync(string key, AuthenticationTicket ticket);
         Task<AuthenticationTicket> RetrieveAsync(string key);
         Task<string> StoreAsync(AuthenticationTicket ticket);
     }
     public class PostConfigureCookieAuthenticationOptions : IPostConfigureOptions<CookieAuthenticationOptions> {
         public PostConfigureCookieAuthenticationOptions(IDataProtectionProvider dataProtection);
         public void PostConfigure(string name, CookieAuthenticationOptions options);
     }
 }
```

