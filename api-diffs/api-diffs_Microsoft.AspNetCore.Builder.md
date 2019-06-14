# Microsoft.AspNetCore.Builder

``` diff
 namespace Microsoft.AspNetCore.Builder {
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
+    public static class CorsEndpointConventionBuilderExtensions {
+        public static TBuilder RequireCors<TBuilder>(this TBuilder builder, Action<CorsPolicyBuilder> configurePolicy) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder RequireCors<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder;
+    }
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
-    public static class GoogleAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app, GoogleOptions options);

-    }
+    public static class HealthCheckEndpointRouteBuilderExtensions {
+        public static IEndpointConventionBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints, string pattern);
+        public static IEndpointConventionBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints, string pattern, HealthCheckOptions options);
+    }
+    public static class HubEndpointRouteBuilderExtensions {
+        public static HubEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder endpoints, string pattern) where THub : Hub;
+        public static HubEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub;
+    }
+    public interface IEndpointConventionBuilder {
+        void Add(Action<EndpointBuilder> convention);
+    }
     public class IISServerOptions {
+        public bool AllowSynchronousIO { get; set; }
+        public Nullable<long> MaxRequestBodySize { get; set; }
     }
-    public static class JwtBearerAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app, JwtBearerOptions options);

-    }
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
+    public static class RequestLocalizationOptionsExtensions {
+        public static RequestLocalizationOptions AddInitialRequestCultureProvider(this RequestLocalizationOptions requestLocalizationOptions, RequestCultureProvider requestCultureProvider);
+    }
+    public static class RoutingEndpointConventionBuilderExtensions {
+        public static TBuilder RequireHost<TBuilder>(this TBuilder builder, params string[] hosts) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, Func<EndpointBuilder, string> func) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, string displayName) where TBuilder : IEndpointConventionBuilder;
+        public static TBuilder WithMetadata<TBuilder>(this TBuilder builder, params object[] items) where TBuilder : IEndpointConventionBuilder;
+    }
     public class SessionOptions {
-        public string CookieDomain { get; set; }

-        public bool CookieHttpOnly { get; set; }

-        public string CookieName { get; set; }

-        public string CookiePath { get; set; }

-        public CookieSecurePolicy CookieSecure { get; set; }

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
     public class StaticFileOptions : SharedOptionsBase {
+        public HttpsCompressionMode HttpsCompression { get; set; }
     }
+    public static class StaticFilesEndpointRouteBuilderExtensions {
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string filePath);
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string filePath, StaticFileOptions options);
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string pattern, string filePath);
+        public static IEndpointConventionBuilder MapFallbackToFile(this IEndpointRouteBuilder endpoints, string pattern, string filePath, StaticFileOptions options);
+    }
-    public static class TwitterAppBuilderExtensions {
 {
-        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app);

-        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app, TwitterOptions options);

-    }
-    public static class WebpackDevMiddleware {
 {
-        public static void UseWebpackDevMiddleware(this IApplicationBuilder appBuilder, WebpackDevMiddlewareOptions options = null);

-    }
 }
```

