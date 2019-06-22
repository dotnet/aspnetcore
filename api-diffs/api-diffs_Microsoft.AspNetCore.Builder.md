# Microsoft.AspNetCore.Builder

``` diff
 namespace Microsoft.AspNetCore.Builder {
+    public class ApplicationBuilder : IApplicationBuilder {
+        public ApplicationBuilder(IServiceProvider serviceProvider);
+        public ApplicationBuilder(IServiceProvider serviceProvider, object server);
+        public IServiceProvider ApplicationServices { get; set; }
+        public IDictionary<string, object> Properties { get; }
+        public IFeatureCollection ServerFeatures { get; }
+        public RequestDelegate Build();
+        public IApplicationBuilder New();
+        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
+    }
     public static class ApplicationBuilderExtensions {
         public static IApplicationBuilder UseRequestLocalization(this IApplicationBuilder app);
         public static IApplicationBuilder UseRequestLocalization(this IApplicationBuilder app, RequestLocalizationOptions options);
         public static IApplicationBuilder UseRequestLocalization(this IApplicationBuilder app, Action<RequestLocalizationOptions> optionsAction);
         public static IApplicationBuilder UseRequestLocalization(this IApplicationBuilder app, params string[] cultures);
     }
     public static class AuthAppBuilderExtensions {
         public static IApplicationBuilder UseAuthentication(this IApplicationBuilder app);
     }
+    public static class AuthorizationAppBuilderExtensions {
+        public static IApplicationBuilder UseAuthorization(this IApplicationBuilder app);
+    }
+    public static class AuthorizationEndpointConventionBuilderExtensions {
+        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params IAuthorizeData[] authorizeData) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, params string[] policyNames) where TBuilder : IEndpointConventionBuilder;
+    }
-    public static class BuilderExtensions {
 {
-        public static IApplicationBuilder UseIdentity(this IApplicationBuilder app);

-    }
+    public static class CertificateForwardingBuilderExtensions {
+        public static IApplicationBuilder UseCertificateForwarding(this IApplicationBuilder app);
+    }
+    public static class ComponentEndpointConventionBuilderExtensions {
+        public static ComponentEndpointConventionBuilder AddComponent(this ComponentEndpointConventionBuilder builder, Type componentType, string selector);
+    }
+    public static class ComponentEndpointRouteBuilderExtensions {
+        public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints);
+        public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints, Action<HttpConnectionDispatcherOptions> configureOptions);
+        public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints, Type type, string selector);
+        public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints, Type type, string selector, Action<HttpConnectionDispatcherOptions> configureOptions);
+        public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints, Type componentType, string selector, string path);
+        public static ComponentEndpointConventionBuilder MapBlazorHub(this IEndpointRouteBuilder endpoints, Type componentType, string selector, string path, Action<HttpConnectionDispatcherOptions> configureOptions);
+        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this IEndpointRouteBuilder endpoints, string selector) where TComponent : IComponent;
+        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this IEndpointRouteBuilder endpoints, string selector, Action<HttpConnectionDispatcherOptions> configureOptions) where TComponent : IComponent;
+        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this IEndpointRouteBuilder endpoints, string selector, string path) where TComponent : IComponent;
+        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(this IEndpointRouteBuilder endpoints, string selector, string path, Action<HttpConnectionDispatcherOptions> configureOptions) where TComponent : IComponent;
+    }
+    public static class ConnectionEndpointRouteBuilderExtensions {
+        public static IEndpointConventionBuilder MapConnectionHandler<TConnectionHandler>(this IEndpointRouteBuilder endpoints, string pattern) where TConnectionHandler : ConnectionHandler;
+        public static IEndpointConventionBuilder MapConnectionHandler<TConnectionHandler>(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : ConnectionHandler;
+        public static IEndpointConventionBuilder MapConnections(this IEndpointRouteBuilder endpoints, string pattern, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure);
+        public static IEndpointConventionBuilder MapConnections(this IEndpointRouteBuilder endpoints, string pattern, Action<IConnectionBuilder> configure);
+    }
     public static class ConnectionsAppBuilderExtensions {
         public static IApplicationBuilder UseConnections(this IApplicationBuilder app, Action<ConnectionsRouteBuilder> configure);
     }
+    public sealed class ControllerActionEndpointConventionBuilder : IEndpointConventionBuilder {
+        public void Add(Action<EndpointBuilder> convention);
+    }
+    public static class ControllerEndpointRouteBuilderExtensions {
+        public static ControllerActionEndpointConventionBuilder MapAreaControllerRoute(this IEndpointRouteBuilder endpoints, string name, string areaName, string pattern, object defaults = null, object constraints = null, object dataTokens = null);
+        public static ControllerActionEndpointConventionBuilder MapControllerRoute(this IEndpointRouteBuilder endpoints, string name, string pattern, object defaults = null, object constraints = null, object dataTokens = null);
+        public static ControllerActionEndpointConventionBuilder MapControllers(this IEndpointRouteBuilder endpoints);
+        public static ControllerActionEndpointConventionBuilder MapDefaultControllerRoute(this IEndpointRouteBuilder endpoints);
+        public static IEndpointConventionBuilder MapFallbackToAreaController(this IEndpointRouteBuilder endpoints, string action, string controller, string area);
+        public static IEndpointConventionBuilder MapFallbackToAreaController(this IEndpointRouteBuilder endpoints, string pattern, string action, string controller, string area);
+        public static IEndpointConventionBuilder MapFallbackToController(this IEndpointRouteBuilder endpoints, string action, string controller);
+        public static IEndpointConventionBuilder MapFallbackToController(this IEndpointRouteBuilder endpoints, string pattern, string action, string controller);
+    }
-    public static class CookieAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseCookieAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseCookieAuthentication(this IApplicationBuilder app, CookieAuthenticationOptions options);

-    }
     public static class CookiePolicyAppBuilderExtensions {
         public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app);
         public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app, CookiePolicyOptions options);
     }
     public class CookiePolicyOptions {
         public CookiePolicyOptions();
         public Func<HttpContext, bool> CheckConsentNeeded { get; set; }
         public CookieBuilder ConsentCookie { get; set; }
         public HttpOnlyPolicy HttpOnly { get; set; }
         public SameSiteMode MinimumSameSitePolicy { get; set; }
         public Action<AppendCookieContext> OnAppendCookie { get; set; }
         public Action<DeleteCookieContext> OnDeleteCookie { get; set; }
         public CookieSecurePolicy Secure { get; set; }
     }
+    public static class CorsEndpointConventionBuilderExtensions {
+        public static TBuilder RequireCors<TBuilder>(this TBuilder builder, Action<CorsPolicyBuilder> configurePolicy) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder RequireCors<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder;
+    }
     public static class CorsMiddlewareExtensions {
         public static IApplicationBuilder UseCors(this IApplicationBuilder app);
         public static IApplicationBuilder UseCors(this IApplicationBuilder app, Action<CorsPolicyBuilder> configurePolicy);
         public static IApplicationBuilder UseCors(this IApplicationBuilder app, string policyName);
     }
-    public static class DatabaseErrorPageExtensions {
 {
-        public static IApplicationBuilder UseDatabaseErrorPage(this IApplicationBuilder app);

-        public static IApplicationBuilder UseDatabaseErrorPage(this IApplicationBuilder app, DatabaseErrorPageOptions options);

-    }
-    public class DatabaseErrorPageOptions {
 {
-        public DatabaseErrorPageOptions();

-        public virtual PathString MigrationsEndPointPath { get; set; }

-    }
     public static class DefaultFilesExtensions {
         public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder app);
         public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder app, DefaultFilesOptions options);
         public static IApplicationBuilder UseDefaultFiles(this IApplicationBuilder app, string requestPath);
     }
     public class DefaultFilesOptions : SharedOptionsBase {
         public DefaultFilesOptions();
         public DefaultFilesOptions(SharedOptions sharedOptions);
         public IList<string> DefaultFileNames { get; set; }
     }
     public static class DeveloperExceptionPageExtensions {
         public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder app);
         public static IApplicationBuilder UseDeveloperExceptionPage(this IApplicationBuilder app, DeveloperExceptionPageOptions options);
     }
     public class DeveloperExceptionPageOptions {
         public DeveloperExceptionPageOptions();
         public IFileProvider FileProvider { get; set; }
         public int SourceCodeLineCount { get; set; }
     }
     public static class DirectoryBrowserExtensions {
         public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder app);
         public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder app, DirectoryBrowserOptions options);
         public static IApplicationBuilder UseDirectoryBrowser(this IApplicationBuilder app, string requestPath);
     }
     public class DirectoryBrowserOptions : SharedOptionsBase {
         public DirectoryBrowserOptions();
         public DirectoryBrowserOptions(SharedOptions sharedOptions);
         public IDirectoryFormatter Formatter { get; set; }
     }
+    public abstract class EndpointBuilder {
+        protected EndpointBuilder();
+        public string DisplayName { get; set; }
+        public IList<object> Metadata { get; }
+        public RequestDelegate RequestDelegate { get; set; }
+        public abstract Endpoint Build();
+    }
+    public static class EndpointRouteBuilderExtensions {
+        public static IEndpointConventionBuilder Map(this IEndpointRouteBuilder endpoints, RoutePattern pattern, RequestDelegate requestDelegate);
+        public static IEndpointConventionBuilder Map(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate requestDelegate);
+        public static IEndpointConventionBuilder MapDelete(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate requestDelegate);
+        public static IEndpointConventionBuilder MapGet(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate requestDelegate);
+        public static IEndpointConventionBuilder MapMethods(this IEndpointRouteBuilder endpoints, string pattern, IEnumerable<string> httpMethods, RequestDelegate requestDelegate);
+        public static IEndpointConventionBuilder MapPost(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate requestDelegate);
+        public static IEndpointConventionBuilder MapPut(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate requestDelegate);
+    }
+    public static class EndpointRoutingApplicationBuilderExtensions {
+        public static IApplicationBuilder UseEndpoints(this IApplicationBuilder builder, Action<IEndpointRouteBuilder> configure);
+        public static IApplicationBuilder UseRouting(this IApplicationBuilder builder);
+    }
     public static class ExceptionHandlerExtensions {
         public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app);
         public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, ExceptionHandlerOptions options);
         public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, Action<IApplicationBuilder> configure);
         public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, string errorHandlingPath);
     }
     public class ExceptionHandlerOptions {
         public ExceptionHandlerOptions();
         public RequestDelegate ExceptionHandler { get; set; }
         public PathString ExceptionHandlingPath { get; set; }
     }
-    public static class FacebookAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseFacebookAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseFacebookAuthentication(this IApplicationBuilder app, FacebookOptions options);

-    }
+    public static class FallbackEndpointRouteBuilderExtensions {
+        public static readonly string DefaultPattern;
+        public static IEndpointConventionBuilder MapFallback(this IEndpointRouteBuilder endpoints, RequestDelegate requestDelegate);
+        public static IEndpointConventionBuilder MapFallback(this IEndpointRouteBuilder endpoints, string pattern, RequestDelegate requestDelegate);
+    }
     public static class FileServerExtensions {
         public static IApplicationBuilder UseFileServer(this IApplicationBuilder app);
         public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, FileServerOptions options);
         public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, bool enableDirectoryBrowsing);
         public static IApplicationBuilder UseFileServer(this IApplicationBuilder app, string requestPath);
     }
     public class FileServerOptions : SharedOptionsBase {
         public FileServerOptions();
         public DefaultFilesOptions DefaultFilesOptions { get; private set; }
         public DirectoryBrowserOptions DirectoryBrowserOptions { get; private set; }
         public bool EnableDefaultFiles { get; set; }
         public bool EnableDirectoryBrowsing { get; set; }
         public StaticFileOptions StaticFileOptions { get; private set; }
     }
     public static class ForwardedHeadersExtensions {
         public static IApplicationBuilder UseForwardedHeaders(this IApplicationBuilder builder);
         public static IApplicationBuilder UseForwardedHeaders(this IApplicationBuilder builder, ForwardedHeadersOptions options);
     }
     public class ForwardedHeadersOptions {
         public ForwardedHeadersOptions();
         public IList<string> AllowedHosts { get; set; }
         public string ForwardedForHeaderName { get; set; }
         public ForwardedHeaders ForwardedHeaders { get; set; }
         public string ForwardedHostHeaderName { get; set; }
         public string ForwardedProtoHeaderName { get; set; }
         public Nullable<int> ForwardLimit { get; set; }
         public IList<IPNetwork> KnownNetworks { get; }
         public IList<IPAddress> KnownProxies { get; }
         public string OriginalForHeaderName { get; set; }
         public string OriginalHostHeaderName { get; set; }
         public string OriginalProtoHeaderName { get; set; }
         public bool RequireHeaderSymmetry { get; set; }
     }
-    public static class GoogleAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app, GoogleOptions options);

-    }
     public static class HealthCheckApplicationBuilderExtensions {
         public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path);
         public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, HealthCheckOptions options);
         public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, int port);
         public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, int port, HealthCheckOptions options);
         public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, string port);
         public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path, string port, HealthCheckOptions options);
     }
+    public static class HealthCheckEndpointRouteBuilderExtensions {
+        public static IEndpointConventionBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints, string pattern);
+        public static IEndpointConventionBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints, string pattern, HealthCheckOptions options);
+    }
     public static class HostFilteringBuilderExtensions {
         public static IApplicationBuilder UseHostFiltering(this IApplicationBuilder app);
     }
     public static class HostFilteringServicesExtensions {
         public static IServiceCollection AddHostFiltering(this IServiceCollection services, Action<HostFilteringOptions> configureOptions);
     }
     public static class HstsBuilderExtensions {
         public static IApplicationBuilder UseHsts(this IApplicationBuilder app);
     }
     public static class HstsServicesExtensions {
         public static IServiceCollection AddHsts(this IServiceCollection services, Action<HstsOptions> configureOptions);
     }
     public static class HttpMethodOverrideExtensions {
         public static IApplicationBuilder UseHttpMethodOverride(this IApplicationBuilder builder);
         public static IApplicationBuilder UseHttpMethodOverride(this IApplicationBuilder builder, HttpMethodOverrideOptions options);
     }
     public class HttpMethodOverrideOptions {
         public HttpMethodOverrideOptions();
         public string FormFieldName { get; set; }
     }
     public static class HttpsPolicyBuilderExtensions {
         public static IApplicationBuilder UseHttpsRedirection(this IApplicationBuilder app);
     }
     public static class HttpsRedirectionServicesExtensions {
         public static IServiceCollection AddHttpsRedirection(this IServiceCollection services, Action<HttpsRedirectionOptions> configureOptions);
     }
+    public static class HubEndpointRouteBuilderExtensions {
+        public static HubEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder endpoints, string pattern) where THub : Hub;
+        public static HubEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub;
+    }
     public interface IApplicationBuilder {
         IServiceProvider ApplicationServices { get; set; }
         IDictionary<string, object> Properties { get; }
         IFeatureCollection ServerFeatures { get; }
         RequestDelegate Build();
         IApplicationBuilder New();
         IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
     }
+    public interface IEndpointConventionBuilder {
+        void Add(Action<EndpointBuilder> convention);
+    }
     public class IISOptions {
         public IISOptions();
         public string AuthenticationDisplayName { get; set; }
         public bool AutomaticAuthentication { get; set; }
         public bool ForwardClientCertificate { get; set; }
     }
     public class IISServerOptions {
         public IISServerOptions();
+        public bool AllowSynchronousIO { get; set; }
         public string AuthenticationDisplayName { get; set; }
         public bool AutomaticAuthentication { get; set; }
+        public Nullable<long> MaxRequestBodySize { get; set; }
     }
-    public static class JwtBearerAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app, JwtBearerOptions options);

-    }
     public static class MapExtensions {
         public static IApplicationBuilder Map(this IApplicationBuilder app, PathString pathMatch, Action<IApplicationBuilder> configuration);
     }
     public static class MapRouteRouteBuilderExtensions {
         public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string name, string template);
         public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults);
         public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints);
         public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints, object dataTokens);
     }
     public static class MapWhenExtensions {
         public static IApplicationBuilder MapWhen(this IApplicationBuilder app, Func<HttpContext, bool> predicate, Action<IApplicationBuilder> configuration);
     }
-    public static class MicrosoftAccountAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseMicrosoftAccountAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseMicrosoftAccountAuthentication(this IApplicationBuilder app, MicrosoftAccountOptions options);

-    }
-    public static class MigrationsEndPointExtensions {
 {
-        public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app);

-        public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app, MigrationsEndPointOptions options);

-    }
-    public class MigrationsEndPointOptions {
 {
-        public static PathString DefaultPath;

-        public MigrationsEndPointOptions();

-        public virtual PathString Path { get; set; }

-    }
     public static class MvcApplicationBuilderExtensions {
         public static IApplicationBuilder UseMvc(this IApplicationBuilder app);
         public static IApplicationBuilder UseMvc(this IApplicationBuilder app, Action<IRouteBuilder> configureRoutes);
         public static IApplicationBuilder UseMvcWithDefaultRoute(this IApplicationBuilder app);
     }
     public static class MvcAreaRouteBuilderExtensions {
         public static IRouteBuilder MapAreaRoute(this IRouteBuilder routeBuilder, string name, string areaName, string template);
         public static IRouteBuilder MapAreaRoute(this IRouteBuilder routeBuilder, string name, string areaName, string template, object defaults);
         public static IRouteBuilder MapAreaRoute(this IRouteBuilder routeBuilder, string name, string areaName, string template, object defaults, object constraints);
         public static IRouteBuilder MapAreaRoute(this IRouteBuilder routeBuilder, string name, string areaName, string template, object defaults, object constraints, object dataTokens);
     }
-    public static class OAuthAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseOAuthAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseOAuthAuthentication(this IApplicationBuilder app, OAuthOptions options);

-    }
-    public static class OpenIdConnectAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app, OpenIdConnectOptions options);

-    }
-    public static class OwinExtensions {
 {
-        public static IApplicationBuilder UseBuilder(this Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>> app);

-        public static Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>> UseBuilder(this Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>> app, Action<IApplicationBuilder> pipeline);

-        public static Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>> UseBuilder(this Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>> app, Action<IApplicationBuilder> pipeline, IServiceProvider serviceProvider);

-        public static IApplicationBuilder UseBuilder(this Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>> app, IServiceProvider serviceProvider);

-        public static Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>> UseOwin(this IApplicationBuilder builder);

-        public static IApplicationBuilder UseOwin(this IApplicationBuilder builder, Action<Action<Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>>> pipeline);

-    }
+    public sealed class PageActionEndpointConventionBuilder : IEndpointConventionBuilder {
+        public void Add(Action<EndpointBuilder> convention);
+    }
+    public static class RazorPagesEndpointRouteBuilderExtensions {
+        public static IEndpointConventionBuilder MapFallbackToAreaPage(this IEndpointRouteBuilder endpoints, string page, string area);
+        public static IEndpointConventionBuilder MapFallbackToAreaPage(this IEndpointRouteBuilder endpoints, string pattern, string page, string area);
+        public static IEndpointConventionBuilder MapFallbackToPage(this IEndpointRouteBuilder endpoints, string page);
+        public static IEndpointConventionBuilder MapFallbackToPage(this IEndpointRouteBuilder endpoints, string pattern, string page);
+        public static PageActionEndpointConventionBuilder MapRazorPages(this IEndpointRouteBuilder endpoints);
+    }
     public class RequestLocalizationOptions {
         public RequestLocalizationOptions();
         public RequestCulture DefaultRequestCulture { get; set; }
         public bool FallBackToParentCultures { get; set; }
         public bool FallBackToParentUICultures { get; set; }
         public IList<IRequestCultureProvider> RequestCultureProviders { get; set; }
         public IList<CultureInfo> SupportedCultures { get; set; }
         public IList<CultureInfo> SupportedUICultures { get; set; }
         public RequestLocalizationOptions AddSupportedCultures(params string[] cultures);
         public RequestLocalizationOptions AddSupportedUICultures(params string[] uiCultures);
         public RequestLocalizationOptions SetDefaultCulture(string defaultCulture);
     }
+    public static class RequestLocalizationOptionsExtensions {
+        public static RequestLocalizationOptions AddInitialRequestCultureProvider(this RequestLocalizationOptions requestLocalizationOptions, RequestCultureProvider requestCultureProvider);
+    }
     public static class ResponseCachingExtensions {
         public static IApplicationBuilder UseResponseCaching(this IApplicationBuilder app);
     }
     public static class ResponseCompressionBuilderExtensions {
         public static IApplicationBuilder UseResponseCompression(this IApplicationBuilder builder);
     }
     public static class ResponseCompressionServicesExtensions {
         public static IServiceCollection AddResponseCompression(this IServiceCollection services);
         public static IServiceCollection AddResponseCompression(this IServiceCollection services, Action<ResponseCompressionOptions> configureOptions);
     }
     public static class RewriteBuilderExtensions {
         public static IApplicationBuilder UseRewriter(this IApplicationBuilder app);
         public static IApplicationBuilder UseRewriter(this IApplicationBuilder app, RewriteOptions options);
     }
     public class RouterMiddleware {
         public RouterMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IRouter router);
         public Task Invoke(HttpContext httpContext);
     }
     public static class RoutingBuilderExtensions {
         public static IApplicationBuilder UseRouter(this IApplicationBuilder builder, IRouter router);
         public static IApplicationBuilder UseRouter(this IApplicationBuilder builder, Action<IRouteBuilder> action);
     }
+    public static class RoutingEndpointConventionBuilderExtensions {
+        public static TBuilder RequireHost<TBuilder>(this TBuilder builder, params string[] hosts) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, Func<EndpointBuilder, string> func) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, string displayName) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder WithMetadata<TBuilder>(this TBuilder builder, params object[] items) where TBuilder : IEndpointConventionBuilder;
+    }
     public static class RunExtensions {
         public static void Run(this IApplicationBuilder app, RequestDelegate handler);
     }
     public static class SessionMiddlewareExtensions {
         public static IApplicationBuilder UseSession(this IApplicationBuilder app);
         public static IApplicationBuilder UseSession(this IApplicationBuilder app, SessionOptions options);
     }
     public class SessionOptions {
         public SessionOptions();
         public CookieBuilder Cookie { get; set; }
-        public string CookieDomain { get; set; }

-        public bool CookieHttpOnly { get; set; }

-        public string CookieName { get; set; }

-        public string CookiePath { get; set; }

-        public CookieSecurePolicy CookieSecure { get; set; }

         public TimeSpan IdleTimeout { get; set; }
         public TimeSpan IOTimeout { get; set; }
     }
     public static class SignalRAppBuilderExtensions {
         public static IApplicationBuilder UseSignalR(this IApplicationBuilder app, Action<HubRouteBuilder> configure);
     }
-    public static class SpaApplicationBuilderExtensions {
 {
-        public static void UseSpa(this IApplicationBuilder app, Action<ISpaBuilder> configuration);

-    }
-    public static class SpaPrerenderingExtensions {
 {
-        public static void UseSpaPrerendering(this ISpaBuilder spaBuilder, Action<SpaPrerenderingOptions> configuration);

-    }
-    public class SpaPrerenderingOptions {
 {
-        public SpaPrerenderingOptions();

-        public ISpaPrerendererBuilder BootModuleBuilder { get; set; }

-        public string BootModulePath { get; set; }

-        public string[] ExcludeUrls { get; set; }

-        public Action<HttpContext, IDictionary<string, object>> SupplyData { get; set; }

-    }
-    public static class SpaProxyingExtensions {
 {
-        public static void UseProxyToSpaDevelopmentServer(this ISpaBuilder spaBuilder, Func<Task<Uri>> baseUriTaskFactory);

-        public static void UseProxyToSpaDevelopmentServer(this ISpaBuilder spaBuilder, string baseUri);

-        public static void UseProxyToSpaDevelopmentServer(this ISpaBuilder spaBuilder, Uri baseUri);

-    }
-    public static class SpaRouteExtensions {
 {
-        public static void MapSpaFallbackRoute(this IRouteBuilder routeBuilder, string name, object defaults, object constraints = null, object dataTokens = null);

-        public static void MapSpaFallbackRoute(this IRouteBuilder routeBuilder, string name, string templatePrefix, object defaults, object constraints = null, object dataTokens = null);

-    }
     public static class StaticFileExtensions {
         public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app);
         public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app, StaticFileOptions options);
         public static IApplicationBuilder UseStaticFiles(this IApplicationBuilder app, string requestPath);
     }
     public class StaticFileOptions : SharedOptionsBase {
         public StaticFileOptions();
         public StaticFileOptions(SharedOptions sharedOptions);
         public IContentTypeProvider ContentTypeProvider { get; set; }
         public string DefaultContentType { get; set; }
+        public HttpsCompressionMode HttpsCompression { get; set; }
         public Action<StaticFileResponseContext> OnPrepareResponse { get; set; }
         public bool ServeUnknownFileTypes { get; set; }
     }
+    public static class StaticFilesEndpointRouteBuilderExtensions {
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string filePath);
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string filePath, StaticFileOptions options);
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string pattern, string filePath);
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string pattern, string filePath, StaticFileOptions options);
+    }
     public static class StatusCodePagesExtensions {
         public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app);
         public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, StatusCodePagesOptions options);
         public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, Action<IApplicationBuilder> configuration);
         public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, Func<StatusCodeContext, Task> handler);
         public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, string contentType, string bodyFormat);
         public static IApplicationBuilder UseStatusCodePagesWithRedirects(this IApplicationBuilder app, string locationFormat);
         public static IApplicationBuilder UseStatusCodePagesWithReExecute(this IApplicationBuilder app, string pathFormat, string queryFormat = null);
     }
     public class StatusCodePagesOptions {
         public StatusCodePagesOptions();
         public Func<StatusCodeContext, Task> HandleAsync { get; set; }
     }
-    public static class TwitterAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app, TwitterOptions options);

-    }
     public static class UseExtensions {
         public static IApplicationBuilder Use(this IApplicationBuilder app, Func<HttpContext, Func<Task>, Task> middleware);
     }
     public static class UseMiddlewareExtensions {
         public static IApplicationBuilder UseMiddleware(this IApplicationBuilder app, Type middleware, params object[] args);
         public static IApplicationBuilder UseMiddleware<TMiddleware>(this IApplicationBuilder app, params object[] args);
     }
     public static class UsePathBaseExtensions {
         public static IApplicationBuilder UsePathBase(this IApplicationBuilder app, PathString pathBase);
     }
     public static class UseWhenExtensions {
         public static IApplicationBuilder UseWhen(this IApplicationBuilder app, Func<HttpContext, bool> predicate, Action<IApplicationBuilder> configuration);
     }
-    public static class WebpackDevMiddleware {
 {
-        public static void UseWebpackDevMiddleware(this IApplicationBuilder appBuilder, WebpackDevMiddlewareOptions options = null);

-    }
     public static class WebSocketMiddlewareExtensions {
         public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app);
         public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app, WebSocketOptions options);
     }
     public class WebSocketOptions {
         public WebSocketOptions();
         public IList<string> AllowedOrigins { get; }
         public TimeSpan KeepAliveInterval { get; set; }
         public int ReceiveBufferSize { get; set; }
     }
     public static class WelcomePageExtensions {
         public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app);
         public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, WelcomePageOptions options);
         public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, PathString path);
         public static IApplicationBuilder UseWelcomePage(this IApplicationBuilder app, string path);
     }
     public class WelcomePageOptions {
         public WelcomePageOptions();
         public PathString Path { get; set; }
     }
 }
```

