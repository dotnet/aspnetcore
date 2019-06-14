# Microsoft.Extensions.DependencyInjection

``` diff
 namespace Microsoft.Extensions.DependencyInjection {
-    public static class AnalysisServiceCollectionExtensions {
 {
-        public static IServiceCollection AddMiddlewareAnalysis(this IServiceCollection services);

-    }
     public static class AuthenticationServiceCollectionExtensions {
-        public static IServiceCollection AddRemoteScheme<TOptions, THandler>(this IServiceCollection services, string authenticationScheme, string displayName, Action<TOptions> configureOptions) where TOptions : RemoteAuthenticationOptions, new() where THandler : RemoteAuthenticationHandler<TOptions>;

-        public static IServiceCollection AddScheme<TOptions, THandler>(this IServiceCollection services, string authenticationScheme, Action<TOptions> configureOptions) where TOptions : AuthenticationSchemeOptions, new() where THandler : AuthenticationHandler<TOptions>;

-        public static IServiceCollection AddScheme<TOptions, THandler>(this IServiceCollection services, string authenticationScheme, string displayName, Action<AuthenticationSchemeBuilder> configureScheme, Action<TOptions> configureOptions) where TOptions : AuthenticationSchemeOptions, new() where THandler : AuthenticationHandler<TOptions>;

-        public static IServiceCollection AddScheme<TOptions, THandler>(this IServiceCollection services, string authenticationScheme, string displayName, Action<TOptions> configureOptions) where TOptions : AuthenticationSchemeOptions, new() where THandler : AuthenticationHandler<TOptions>;

     }
     public static class AuthorizationServiceCollectionExtensions {
-        public static IServiceCollection AddAuthorization(this IServiceCollection services);

-        public static IServiceCollection AddAuthorization(this IServiceCollection services, Action<AuthorizationOptions> configure);

+        public static IServiceCollection AddAuthorizationCore(this IServiceCollection services);
+        public static IServiceCollection AddAuthorizationCore(this IServiceCollection services, Action<AuthorizationOptions> configure);
     }
+    public static class CertificateForwardingServiceExtensions {
+        public static IServiceCollection AddCertificateForwarding(this IServiceCollection services, Action<CertificateForwardingOptions> configure);
+    }
+    public static class ComponentServiceCollectionExtensions {
+        public static IServerSideBlazorBuilder AddServerSideBlazor(this IServiceCollection services);
+    }
     public static class ConnectionsDependencyInjectionExtensions {
+        public static IServiceCollection AddConnections(this IServiceCollection services, Action<ConnectionOptions> options);
     }
-    public static class EntityFrameworkServiceCollectionExtensions {
 {
-        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection serviceCollection, ServiceLifetime contextLifetime, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContext : DbContext;

-        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> optionsAction = null, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContext : DbContext;

-        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection serviceCollection, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContext : DbContext;

-        public static IServiceCollection AddDbContext<TContextService, TContextImplementation>(this IServiceCollection serviceCollection, ServiceLifetime contextLifetime, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContextService : class where TContextImplementation : DbContext, TContextService;

-        public static IServiceCollection AddDbContext<TContextService, TContextImplementation>(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> optionsAction = null, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContextImplementation : DbContext, TContextService;

-        public static IServiceCollection AddDbContext<TContextService, TContextImplementation>(this IServiceCollection serviceCollection, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContextImplementation : DbContext, TContextService;

-        public static IServiceCollection AddDbContextPool<TContext>(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> optionsAction, int poolSize = 128) where TContext : DbContext;

-        public static IServiceCollection AddDbContextPool<TContext>(this IServiceCollection serviceCollection, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction, int poolSize = 128) where TContext : DbContext;

-        public static IServiceCollection AddDbContextPool<TContextService, TContextImplementation>(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> optionsAction, int poolSize = 128) where TContextService : class where TContextImplementation : DbContext, TContextService;

-        public static IServiceCollection AddDbContextPool<TContextService, TContextImplementation>(this IServiceCollection serviceCollection, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction, int poolSize = 128) where TContextService : class where TContextImplementation : DbContext, TContextService;

-    }
-    public static class FacebookAuthenticationOptionsExtensions {
 {
-        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, Action<FacebookOptions> configureOptions);

-        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, string authenticationScheme, Action<FacebookOptions> configureOptions);

-        public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<FacebookOptions> configureOptions);

-    }
-    public static class GoogleExtensions {
 {
-        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, Action<GoogleOptions> configureOptions);

-        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, string authenticationScheme, Action<GoogleOptions> configureOptions);

-        public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GoogleOptions> configureOptions);

-    }
     public static class HealthChecksBuilderAddCheckExtensions {
-        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, IHealthCheck instance, Nullable<HealthStatus> failureStatus = default(Nullable<HealthStatus>), IEnumerable<string> tags = null);
+        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, IHealthCheck instance, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags);
+        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, IHealthCheck instance, Nullable<HealthStatus> failureStatus = default(Nullable<HealthStatus>), IEnumerable<string> tags = null, Nullable<TimeSpan> timeout = default(Nullable<TimeSpan>));
-        public static IHealthChecksBuilder AddCheck<T>(this IHealthChecksBuilder builder, string name, Nullable<HealthStatus> failureStatus = default(Nullable<HealthStatus>), IEnumerable<string> tags = null) where T : class, IHealthCheck;
+        public static IHealthChecksBuilder AddCheck<T>(this IHealthChecksBuilder builder, string name, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags) where T : class, IHealthCheck;
+        public static IHealthChecksBuilder AddCheck<T>(this IHealthChecksBuilder builder, string name, Nullable<HealthStatus> failureStatus = default(Nullable<HealthStatus>), IEnumerable<string> tags = null, Nullable<TimeSpan> timeout = default(Nullable<TimeSpan>)) where T : class, IHealthCheck;
+        public static IHealthChecksBuilder AddTypeActivatedCheck<T>(this IHealthChecksBuilder builder, string name, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags, TimeSpan timeout, params object[] args) where T : class, IHealthCheck;
     }
     public static class HealthChecksBuilderDelegateExtensions {
-        public static IHealthChecksBuilder AddAsyncCheck(this IHealthChecksBuilder builder, string name, Func<CancellationToken, Task<HealthCheckResult>> check, IEnumerable<string> tags = null);
+        public static IHealthChecksBuilder AddAsyncCheck(this IHealthChecksBuilder builder, string name, Func<CancellationToken, Task<HealthCheckResult>> check, IEnumerable<string> tags);
+        public static IHealthChecksBuilder AddAsyncCheck(this IHealthChecksBuilder builder, string name, Func<CancellationToken, Task<HealthCheckResult>> check, IEnumerable<string> tags = null, Nullable<TimeSpan> timeout = default(Nullable<TimeSpan>));
-        public static IHealthChecksBuilder AddAsyncCheck(this IHealthChecksBuilder builder, string name, Func<Task<HealthCheckResult>> check, IEnumerable<string> tags = null);
+        public static IHealthChecksBuilder AddAsyncCheck(this IHealthChecksBuilder builder, string name, Func<Task<HealthCheckResult>> check, IEnumerable<string> tags);
+        public static IHealthChecksBuilder AddAsyncCheck(this IHealthChecksBuilder builder, string name, Func<Task<HealthCheckResult>> check, IEnumerable<string> tags = null, Nullable<TimeSpan> timeout = default(Nullable<TimeSpan>));
-        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, Func<HealthCheckResult> check, IEnumerable<string> tags = null);
+        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, Func<HealthCheckResult> check, IEnumerable<string> tags);
+        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, Func<HealthCheckResult> check, IEnumerable<string> tags = null, Nullable<TimeSpan> timeout = default(Nullable<TimeSpan>));
-        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, Func<CancellationToken, HealthCheckResult> check, IEnumerable<string> tags = null);
+        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, Func<CancellationToken, HealthCheckResult> check, IEnumerable<string> tags);
+        public static IHealthChecksBuilder AddCheck(this IHealthChecksBuilder builder, string name, Func<CancellationToken, HealthCheckResult> check, IEnumerable<string> tags = null, Nullable<TimeSpan> timeout = default(Nullable<TimeSpan>));
     }
-    public static class IdentityEntityFrameworkBuilderExtensions {
 {
-        public static IdentityBuilder AddEntityFrameworkStores<TContext>(this IdentityBuilder builder) where TContext : DbContext;

-    }
-    public static class IdentityServiceCollectionUIExtensions {
 {
-        public static IdentityBuilder AddDefaultIdentity<TUser>(this IServiceCollection services) where TUser : class;

-        public static IdentityBuilder AddDefaultIdentity<TUser>(this IServiceCollection services, Action<IdentityOptions> configureOptions) where TUser : class;

-    }
-    public static class InMemoryServiceCollectionExtensions {
 {
-        public static IServiceCollection AddEntityFrameworkInMemoryDatabase(this IServiceCollection serviceCollection);

-    }
+    public interface IServerSideBlazorBuilder {
+        IServiceCollection Services { get; }
+    }
-    public static class JwtBearerExtensions {
 {
-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions);

-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> configureOptions);

-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<JwtBearerOptions> configureOptions);

-    }
-    public static class MicrosoftAccountExtensions {
 {
-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, Action<MicrosoftAccountOptions> configureOptions);

-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, Action<MicrosoftAccountOptions> configureOptions);

-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<MicrosoftAccountOptions> configureOptions);

-    }
     public static class MvcCoreMvcBuilderExtensions {
+        public static IMvcBuilder AddJsonOptions(this IMvcBuilder builder, Action<JsonOptions> configure);
     }
     public static class MvcCoreMvcCoreBuilderExtensions {
+        public static IMvcCoreBuilder AddJsonOptions(this IMvcCoreBuilder builder, Action<JsonOptions> configure);
     }
-    public static class MvcJsonMvcBuilderExtensions {
 {
-        public static IMvcBuilder AddJsonOptions(this IMvcBuilder builder, Action<MvcJsonOptions> setupAction);

-    }
-    public static class MvcJsonMvcCoreBuilderExtensions {
 {
-        public static IMvcCoreBuilder AddJsonFormatters(this IMvcCoreBuilder builder);

-        public static IMvcCoreBuilder AddJsonFormatters(this IMvcCoreBuilder builder, Action<JsonSerializerSettings> setupAction);

-        public static IMvcCoreBuilder AddJsonOptions(this IMvcCoreBuilder builder, Action<MvcJsonOptions> setupAction);

-    }
-    public static class MvcJsonOptionsExtensions {
 {
-        public static MvcJsonOptions UseCamelCasing(this MvcJsonOptions options, bool processDictionaryKeys);

-        public static MvcJsonOptions UseMemberCasing(this MvcJsonOptions options);

-    }
     public static class MvcServiceCollectionExtensions {
+        public static IMvcBuilder AddControllers(this IServiceCollection services);
+        public static IMvcBuilder AddControllers(this IServiceCollection services, Action<MvcOptions> configure);
+        public static IMvcBuilder AddControllersWithViews(this IServiceCollection services);
+        public static IMvcBuilder AddControllersWithViews(this IServiceCollection services, Action<MvcOptions> configure);
+        public static IMvcBuilder AddRazorPages(this IServiceCollection services);
+        public static IMvcBuilder AddRazorPages(this IServiceCollection services, Action<RazorPagesOptions> configure);
     }
-    public static class NodeServicesServiceCollectionExtensions {
 {
-        public static void AddNodeServices(this IServiceCollection serviceCollection);

-        public static void AddNodeServices(this IServiceCollection serviceCollection, Action<NodeServicesOptions> setupAction);

-    }
-    public static class OpenIdConnectExtensions {
 {
-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, Action<OpenIdConnectOptions> configureOptions);

-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, Action<OpenIdConnectOptions> configureOptions);

-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<OpenIdConnectOptions> configureOptions);

-    }
     public static class PolicyServiceCollectionExtensions {
+        public static IServiceCollection AddAuthorization(this IServiceCollection services);
+        public static IServiceCollection AddAuthorization(this IServiceCollection services, Action<AuthorizationOptions> configure);
     }
-    public static class PrerenderingServiceCollectionExtensions {
 {
-        public static void AddSpaPrerenderer(this IServiceCollection serviceCollection);

-    }
+    public static class ServerSizeBlazorBuilderExtensions {
+        public static IServerSideBlazorBuilder AddHubOptions(this IServerSideBlazorBuilder builder, Action<HubOptions> configure);
+    }
     public static class ServiceCollectionHostedServiceExtensions {
+        public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services, Func<IServiceProvider, THostedService> implementationFactory) where THostedService : class, IHostedService;
     }
     public class ServiceDescriptor {
+        public override string ToString();
     }
-    public sealed class ServiceProvider : IDisposable, IServiceProvider, IServiceProviderEngineCallback {
+    public sealed class ServiceProvider : IAsyncDisposable, IDisposable, IServiceProvider {
+        public ValueTask DisposeAsync();
     }
     public class ServiceProviderOptions {
+        public bool ValidateOnBuild { get; set; }
     }
-    public static class SpaStaticFilesExtensions {
 {
-        public static void AddSpaStaticFiles(this IServiceCollection services, Action<SpaStaticFilesOptions> configuration = null);

-        public static void UseSpaStaticFiles(this IApplicationBuilder applicationBuilder);

-        public static void UseSpaStaticFiles(this IApplicationBuilder applicationBuilder, StaticFileOptions options);

-    }
-    public static class SqlServerCachingServicesExtensions {
 {
-        public static IServiceCollection AddDistributedSqlServerCache(this IServiceCollection services, Action<SqlServerCacheOptions> setupAction);

-    }
-    public static class SqlServerServiceCollectionExtensions {
 {
-        public static IServiceCollection AddEntityFrameworkSqlServer(this IServiceCollection serviceCollection);

-    }
-    public static class TwitterExtensions {
 {
-        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, Action<TwitterOptions> configureOptions);

-        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, Action<TwitterOptions> configureOptions);

-        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TwitterOptions> configureOptions);

-    }
-    public static class WsFederationExtensions {
 {
-        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, Action<WsFederationOptions> configureOptions);

-        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, string authenticationScheme, Action<WsFederationOptions> configureOptions);

-        public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<WsFederationOptions> configureOptions);

-    }
 }
```

