# Microsoft.Extensions.DependencyInjection

``` diff
 namespace Microsoft.Extensions.DependencyInjection {
     public static class ActivatorUtilities {
         public static ObjectFactory CreateFactory(Type instanceType, Type[] argumentTypes);
         public static object CreateInstance(IServiceProvider provider, Type instanceType, params object[] parameters);
         public static T CreateInstance<T>(IServiceProvider provider, params object[] parameters);
         public static object GetServiceOrCreateInstance(IServiceProvider provider, Type type);
         public static T GetServiceOrCreateInstance<T>(IServiceProvider provider);
     }
     public class ActivatorUtilitiesConstructorAttribute : Attribute {
         public ActivatorUtilitiesConstructorAttribute();
     }
-    public static class AnalysisServiceCollectionExtensions {
 {
-        public static IServiceCollection AddMiddlewareAnalysis(this IServiceCollection services);

-    }
     public static class AntiforgeryServiceCollectionExtensions {
         public static IServiceCollection AddAntiforgery(this IServiceCollection services);
         public static IServiceCollection AddAntiforgery(this IServiceCollection services, Action<AntiforgeryOptions> setupAction);
     }
     public static class ApplicationModelConventionExtensions {
         public static void Add(this IList<IApplicationModelConvention> conventions, IActionModelConvention actionModelConvention);
         public static void Add(this IList<IApplicationModelConvention> conventions, IControllerModelConvention controllerModelConvention);
         public static void Add(this IList<IApplicationModelConvention> conventions, IParameterModelBaseConvention parameterModelConvention);
         public static void Add(this IList<IApplicationModelConvention> conventions, IParameterModelConvention parameterModelConvention);
         public static void RemoveType(this IList<IApplicationModelConvention> list, Type type);
         public static void RemoveType<TApplicationModelConvention>(this IList<IApplicationModelConvention> list) where TApplicationModelConvention : IApplicationModelConvention;
     }
     public static class AuthenticationCoreServiceCollectionExtensions {
         public static IServiceCollection AddAuthenticationCore(this IServiceCollection services);
         public static IServiceCollection AddAuthenticationCore(this IServiceCollection services, Action<AuthenticationOptions> configureOptions);
     }
     public static class AuthenticationServiceCollectionExtensions {
         public static AuthenticationBuilder AddAuthentication(this IServiceCollection services);
         public static AuthenticationBuilder AddAuthentication(this IServiceCollection services, Action<AuthenticationOptions> configureOptions);
         public static AuthenticationBuilder AddAuthentication(this IServiceCollection services, string defaultScheme);
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
+        public static IServerSideBlazorBuilder AddServerSideBlazor(this IServiceCollection services, Action<CircuitOptions> configure = null);
+    }
     public static class ConnectionsDependencyInjectionExtensions {
         public static IServiceCollection AddConnections(this IServiceCollection services);
+        public static IServiceCollection AddConnections(this IServiceCollection services, Action<ConnectionOptions> options);
     }
     public static class CookieExtensions {
         public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder);
         public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, Action<CookieAuthenticationOptions> configureOptions);
         public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme);
         public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme, Action<CookieAuthenticationOptions> configureOptions);
         public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<CookieAuthenticationOptions> configureOptions);
     }
     public static class CorsServiceCollectionExtensions {
         public static IServiceCollection AddCors(this IServiceCollection services);
         public static IServiceCollection AddCors(this IServiceCollection services, Action<CorsOptions> setupAction);
     }
     public static class DataProtectionServiceCollectionExtensions {
         public static IDataProtectionBuilder AddDataProtection(this IServiceCollection services);
         public static IDataProtectionBuilder AddDataProtection(this IServiceCollection services, Action<DataProtectionOptions> setupAction);
     }
     public class DefaultServiceProviderFactory : IServiceProviderFactory<IServiceCollection> {
         public DefaultServiceProviderFactory();
         public DefaultServiceProviderFactory(ServiceProviderOptions options);
         public IServiceCollection CreateBuilder(IServiceCollection services);
         public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder);
     }
     public static class DirectoryBrowserServiceExtensions {
         public static IServiceCollection AddDirectoryBrowser(this IServiceCollection services);
     }
     public static class EncoderServiceCollectionExtensions {
         public static IServiceCollection AddWebEncoders(this IServiceCollection services);
         public static IServiceCollection AddWebEncoders(this IServiceCollection services, Action<WebEncoderOptions> setupAction);
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
         public static IHealthChecksBuilder AddTypeActivatedCheck<T>(this IHealthChecksBuilder builder, string name, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags, params object[] args) where T : class, IHealthCheck;
+        public static IHealthChecksBuilder AddTypeActivatedCheck<T>(this IHealthChecksBuilder builder, string name, Nullable<HealthStatus> failureStatus, IEnumerable<string> tags, TimeSpan timeout, params object[] args) where T : class, IHealthCheck;
         public static IHealthChecksBuilder AddTypeActivatedCheck<T>(this IHealthChecksBuilder builder, string name, Nullable<HealthStatus> failureStatus, params object[] args) where T : class, IHealthCheck;
         public static IHealthChecksBuilder AddTypeActivatedCheck<T>(this IHealthChecksBuilder builder, string name, params object[] args) where T : class, IHealthCheck;
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
     public static class HealthCheckServiceCollectionExtensions {
         public static IHealthChecksBuilder AddHealthChecks(this IServiceCollection services);
     }
     public static class HttpClientBuilderExtensions {
         public static IHttpClientBuilder AddHttpMessageHandler(this IHttpClientBuilder builder, Func<IServiceProvider, DelegatingHandler> configureHandler);
         public static IHttpClientBuilder AddHttpMessageHandler(this IHttpClientBuilder builder, Func<DelegatingHandler> configureHandler);
         public static IHttpClientBuilder AddHttpMessageHandler<THandler>(this IHttpClientBuilder builder) where THandler : DelegatingHandler;
         public static IHttpClientBuilder AddTypedClient<TClient, TImplementation>(this IHttpClientBuilder builder) where TClient : class where TImplementation : class, TClient;
         public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder) where TClient : class;
         public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder, Func<HttpClient, IServiceProvider, TClient> factory) where TClient : class;
         public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder, Func<HttpClient, TClient> factory) where TClient : class;
         public static IHttpClientBuilder ConfigureHttpClient(this IHttpClientBuilder builder, Action<IServiceProvider, HttpClient> configureClient);
         public static IHttpClientBuilder ConfigureHttpClient(this IHttpClientBuilder builder, Action<HttpClient> configureClient);
         public static IHttpClientBuilder ConfigureHttpMessageHandlerBuilder(this IHttpClientBuilder builder, Action<HttpMessageHandlerBuilder> configureBuilder);
         public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this IHttpClientBuilder builder, Func<IServiceProvider, HttpMessageHandler> configureHandler);
         public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler(this IHttpClientBuilder builder, Func<HttpMessageHandler> configureHandler);
         public static IHttpClientBuilder ConfigurePrimaryHttpMessageHandler<THandler>(this IHttpClientBuilder builder) where THandler : HttpMessageHandler;
         public static IHttpClientBuilder SetHandlerLifetime(this IHttpClientBuilder builder, TimeSpan handlerLifetime);
     }
     public static class HttpClientFactoryServiceCollectionExtensions {
         public static IServiceCollection AddHttpClient(this IServiceCollection services);
         public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name);
         public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient> configureClient);
         public static IHttpClientBuilder AddHttpClient(this IServiceCollection services, string name, Action<HttpClient> configureClient);
         public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services) where TClient : class where TImplementation : class, TClient;
         public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, Action<IServiceProvider, HttpClient> configureClient) where TClient : class where TImplementation : class, TClient;
         public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, Action<HttpClient> configureClient) where TClient : class where TImplementation : class, TClient;
         public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, string name) where TClient : class where TImplementation : class, TClient;
         public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient> configureClient) where TClient : class where TImplementation : class, TClient;
         public static IHttpClientBuilder AddHttpClient<TClient, TImplementation>(this IServiceCollection services, string name, Action<HttpClient> configureClient) where TClient : class where TImplementation : class, TClient;
         public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services) where TClient : class;
         public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, Action<IServiceProvider, HttpClient> configureClient) where TClient : class;
         public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, Action<HttpClient> configureClient) where TClient : class;
         public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, string name) where TClient : class;
         public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, string name, Action<IServiceProvider, HttpClient> configureClient) where TClient : class;
         public static IHttpClientBuilder AddHttpClient<TClient>(this IServiceCollection services, string name, Action<HttpClient> configureClient) where TClient : class;
     }
     public static class HttpServiceCollectionExtensions {
         public static IServiceCollection AddHttpContextAccessor(this IServiceCollection services);
     }
-    public static class IdentityEntityFrameworkBuilderExtensions {
 {
-        public static IdentityBuilder AddEntityFrameworkStores<TContext>(this IdentityBuilder builder) where TContext : DbContext;

-    }
     public static class IdentityServiceCollectionExtensions {
         public static IdentityBuilder AddIdentity<TUser, TRole>(this IServiceCollection services) where TUser : class where TRole : class;
         public static IdentityBuilder AddIdentity<TUser, TRole>(this IServiceCollection services, Action<IdentityOptions> setupAction) where TUser : class where TRole : class;
         public static IdentityBuilder AddIdentityCore<TUser>(this IServiceCollection services) where TUser : class;
         public static IdentityBuilder AddIdentityCore<TUser>(this IServiceCollection services, Action<IdentityOptions> setupAction) where TUser : class;
         public static IServiceCollection ConfigureApplicationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure);
         public static IServiceCollection ConfigureExternalCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure);
     }
-    public static class IdentityServiceCollectionUIExtensions {
 {
-        public static IdentityBuilder AddDefaultIdentity<TUser>(this IServiceCollection services) where TUser : class;

-        public static IdentityBuilder AddDefaultIdentity<TUser>(this IServiceCollection services, Action<IdentityOptions> configureOptions) where TUser : class;

-    }
     public interface IHealthChecksBuilder {
         IServiceCollection Services { get; }
         IHealthChecksBuilder Add(HealthCheckRegistration registration);
     }
     public interface IHttpClientBuilder {
         string Name { get; }
         IServiceCollection Services { get; }
     }
     public interface IMvcBuilder {
         ApplicationPartManager PartManager { get; }
         IServiceCollection Services { get; }
     }
     public interface IMvcCoreBuilder {
         ApplicationPartManager PartManager { get; }
         IServiceCollection Services { get; }
     }
-    public static class InMemoryServiceCollectionExtensions {
 {
-        public static IServiceCollection AddEntityFrameworkInMemoryDatabase(this IServiceCollection serviceCollection);

-    }
+    public interface IServerSideBlazorBuilder {
+        IServiceCollection Services { get; }
+    }
     public interface IServiceCollection : ICollection<ServiceDescriptor>, IEnumerable, IEnumerable<ServiceDescriptor>, IList<ServiceDescriptor>
     public interface IServiceProviderFactory<TContainerBuilder> {
         TContainerBuilder CreateBuilder(IServiceCollection services);
         IServiceProvider CreateServiceProvider(TContainerBuilder containerBuilder);
     }
     public interface IServiceScope : IDisposable {
         IServiceProvider ServiceProvider { get; }
     }
     public interface IServiceScopeFactory {
         IServiceScope CreateScope();
     }
     public interface ISupportRequiredService {
         object GetRequiredService(Type serviceType);
     }
     public static class JsonProtocolDependencyInjectionExtensions {
         public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder) where TBuilder : ISignalRBuilder;
         public static TBuilder AddJsonProtocol<TBuilder>(this TBuilder builder, Action<JsonHubProtocolOptions> configure) where TBuilder : ISignalRBuilder;
     }
-    public static class JwtBearerExtensions {
 {
-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, Action<JwtBearerOptions> configureOptions);

-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> configureOptions);

-        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<JwtBearerOptions> configureOptions);

-    }
     public static class LocalizationServiceCollectionExtensions {
         public static IServiceCollection AddLocalization(this IServiceCollection services);
         public static IServiceCollection AddLocalization(this IServiceCollection services, Action<LocalizationOptions> setupAction);
     }
     public static class LoggingServiceCollectionExtensions {
         public static IServiceCollection AddLogging(this IServiceCollection services);
         public static IServiceCollection AddLogging(this IServiceCollection services, Action<ILoggingBuilder> configure);
     }
     public static class MemoryCacheServiceCollectionExtensions {
         public static IServiceCollection AddDistributedMemoryCache(this IServiceCollection services);
         public static IServiceCollection AddDistributedMemoryCache(this IServiceCollection services, Action<MemoryDistributedCacheOptions> setupAction);
         public static IServiceCollection AddMemoryCache(this IServiceCollection services);
         public static IServiceCollection AddMemoryCache(this IServiceCollection services, Action<MemoryCacheOptions> setupAction);
     }
-    public static class MicrosoftAccountExtensions {
 {
-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, Action<MicrosoftAccountOptions> configureOptions);

-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, Action<MicrosoftAccountOptions> configureOptions);

-        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<MicrosoftAccountOptions> configureOptions);

-    }
     public static class MvcApiExplorerMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddApiExplorer(this IMvcCoreBuilder builder);
     }
     public static class MvcCoreMvcBuilderExtensions {
         public static IMvcBuilder AddApplicationPart(this IMvcBuilder builder, Assembly assembly);
         public static IMvcBuilder AddControllersAsServices(this IMvcBuilder builder);
         public static IMvcBuilder AddFormatterMappings(this IMvcBuilder builder, Action<FormatterMappings> setupAction);
+        public static IMvcBuilder AddJsonOptions(this IMvcBuilder builder, Action<JsonOptions> configure);
         public static IMvcBuilder AddMvcOptions(this IMvcBuilder builder, Action<MvcOptions> setupAction);
         public static IMvcBuilder ConfigureApiBehaviorOptions(this IMvcBuilder builder, Action<ApiBehaviorOptions> setupAction);
         public static IMvcBuilder ConfigureApplicationPartManager(this IMvcBuilder builder, Action<ApplicationPartManager> setupAction);
         public static IMvcBuilder SetCompatibilityVersion(this IMvcBuilder builder, CompatibilityVersion version);
     }
     public static class MvcCoreMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddApplicationPart(this IMvcCoreBuilder builder, Assembly assembly);
         public static IMvcCoreBuilder AddAuthorization(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddAuthorization(this IMvcCoreBuilder builder, Action<AuthorizationOptions> setupAction);
         public static IMvcCoreBuilder AddControllersAsServices(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddFormatterMappings(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddFormatterMappings(this IMvcCoreBuilder builder, Action<FormatterMappings> setupAction);
+        public static IMvcCoreBuilder AddJsonOptions(this IMvcCoreBuilder builder, Action<JsonOptions> configure);
         public static IMvcCoreBuilder AddMvcOptions(this IMvcCoreBuilder builder, Action<MvcOptions> setupAction);
         public static IMvcCoreBuilder ConfigureApiBehaviorOptions(this IMvcCoreBuilder builder, Action<ApiBehaviorOptions> setupAction);
         public static IMvcCoreBuilder ConfigureApplicationPartManager(this IMvcCoreBuilder builder, Action<ApplicationPartManager> setupAction);
         public static IMvcCoreBuilder SetCompatibilityVersion(this IMvcCoreBuilder builder, CompatibilityVersion version);
     }
     public static class MvcCoreServiceCollectionExtensions {
         public static IMvcCoreBuilder AddMvcCore(this IServiceCollection services);
         public static IMvcCoreBuilder AddMvcCore(this IServiceCollection services, Action<MvcOptions> setupAction);
     }
     public static class MvcCorsMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddCors(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddCors(this IMvcCoreBuilder builder, Action<CorsOptions> setupAction);
         public static IMvcCoreBuilder ConfigureCors(this IMvcCoreBuilder builder, Action<CorsOptions> setupAction);
     }
     public static class MvcDataAnnotationsMvcBuilderExtensions {
         public static IMvcBuilder AddDataAnnotationsLocalization(this IMvcBuilder builder);
         public static IMvcBuilder AddDataAnnotationsLocalization(this IMvcBuilder builder, Action<MvcDataAnnotationsLocalizationOptions> setupAction);
     }
     public static class MvcDataAnnotationsMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddDataAnnotations(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddDataAnnotationsLocalization(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddDataAnnotationsLocalization(this IMvcCoreBuilder builder, Action<MvcDataAnnotationsLocalizationOptions> setupAction);
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
     public static class MvcLocalizationMvcBuilderExtensions {
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder);
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder, LanguageViewLocationExpanderFormat format);
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder, LanguageViewLocationExpanderFormat format, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction);
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction, LanguageViewLocationExpanderFormat format);
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction, LanguageViewLocationExpanderFormat format, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcBuilder AddMvcLocalization(this IMvcBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcBuilder AddViewLocalization(this IMvcBuilder builder);
         public static IMvcBuilder AddViewLocalization(this IMvcBuilder builder, LanguageViewLocationExpanderFormat format);
         public static IMvcBuilder AddViewLocalization(this IMvcBuilder builder, LanguageViewLocationExpanderFormat format, Action<LocalizationOptions> setupAction);
         public static IMvcBuilder AddViewLocalization(this IMvcBuilder builder, Action<LocalizationOptions> setupAction);
     }
     public static class MvcLocalizationMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder, LanguageViewLocationExpanderFormat format);
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder, LanguageViewLocationExpanderFormat format, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction);
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction, LanguageViewLocationExpanderFormat format);
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction, LanguageViewLocationExpanderFormat format, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcCoreBuilder AddMvcLocalization(this IMvcCoreBuilder builder, Action<LocalizationOptions> localizationOptionsSetupAction, Action<MvcDataAnnotationsLocalizationOptions> dataAnnotationsLocalizationOptionsSetupAction);
         public static IMvcCoreBuilder AddViewLocalization(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddViewLocalization(this IMvcCoreBuilder builder, LanguageViewLocationExpanderFormat format);
         public static IMvcCoreBuilder AddViewLocalization(this IMvcCoreBuilder builder, LanguageViewLocationExpanderFormat format, Action<LocalizationOptions> setupAction);
         public static IMvcCoreBuilder AddViewLocalization(this IMvcCoreBuilder builder, Action<LocalizationOptions> setupAction);
     }
     public static class MvcRazorMvcBuilderExtensions {
         public static IMvcBuilder AddRazorOptions(this IMvcBuilder builder, Action<RazorViewEngineOptions> setupAction);
         public static IMvcBuilder AddTagHelpersAsServices(this IMvcBuilder builder);
         public static IMvcBuilder InitializeTagHelper<TTagHelper>(this IMvcBuilder builder, Action<TTagHelper, ViewContext> initialize) where TTagHelper : ITagHelper;
     }
     public static class MvcRazorMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddRazorViewEngine(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddRazorViewEngine(this IMvcCoreBuilder builder, Action<RazorViewEngineOptions> setupAction);
         public static IMvcCoreBuilder AddTagHelpersAsServices(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder InitializeTagHelper<TTagHelper>(this IMvcCoreBuilder builder, Action<TTagHelper, ViewContext> initialize) where TTagHelper : ITagHelper;
     }
     public static class MvcRazorPagesMvcBuilderExtensions {
         public static IMvcBuilder AddRazorPagesOptions(this IMvcBuilder builder, Action<RazorPagesOptions> setupAction);
         public static IMvcBuilder WithRazorPagesAtContentRoot(this IMvcBuilder builder);
         public static IMvcBuilder WithRazorPagesRoot(this IMvcBuilder builder, string rootDirectory);
     }
     public static class MvcRazorPagesMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddRazorPages(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddRazorPages(this IMvcCoreBuilder builder, Action<RazorPagesOptions> setupAction);
         public static IMvcCoreBuilder WithRazorPagesRoot(this IMvcCoreBuilder builder, string rootDirectory);
     }
     public static class MvcServiceCollectionExtensions {
+        public static IMvcBuilder AddControllers(this IServiceCollection services);
+        public static IMvcBuilder AddControllers(this IServiceCollection services, Action<MvcOptions> configure);
+        public static IMvcBuilder AddControllersWithViews(this IServiceCollection services);
+        public static IMvcBuilder AddControllersWithViews(this IServiceCollection services, Action<MvcOptions> configure);
         public static IMvcBuilder AddMvc(this IServiceCollection services);
         public static IMvcBuilder AddMvc(this IServiceCollection services, Action<MvcOptions> setupAction);
+        public static IMvcBuilder AddRazorPages(this IServiceCollection services);
+        public static IMvcBuilder AddRazorPages(this IServiceCollection services, Action<RazorPagesOptions> configure);
     }
     public static class MvcViewFeaturesMvcBuilderExtensions {
         public static IMvcBuilder AddCookieTempDataProvider(this IMvcBuilder builder);
         public static IMvcBuilder AddCookieTempDataProvider(this IMvcBuilder builder, Action<CookieTempDataProviderOptions> setupAction);
         public static IMvcBuilder AddSessionStateTempDataProvider(this IMvcBuilder builder);
         public static IMvcBuilder AddViewComponentsAsServices(this IMvcBuilder builder);
         public static IMvcBuilder AddViewOptions(this IMvcBuilder builder, Action<MvcViewOptions> setupAction);
     }
     public static class MvcViewFeaturesMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddCookieTempDataProvider(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddCookieTempDataProvider(this IMvcCoreBuilder builder, Action<CookieTempDataProviderOptions> setupAction);
         public static IMvcCoreBuilder AddViews(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddViews(this IMvcCoreBuilder builder, Action<MvcViewOptions> setupAction);
         public static IMvcCoreBuilder ConfigureViews(this IMvcCoreBuilder builder, Action<MvcViewOptions> setupAction);
     }
     public static class MvcXmlMvcBuilderExtensions {
         public static IMvcBuilder AddXmlDataContractSerializerFormatters(this IMvcBuilder builder);
         public static IMvcBuilder AddXmlDataContractSerializerFormatters(this IMvcBuilder builder, Action<MvcXmlOptions> setupAction);
         public static IMvcBuilder AddXmlOptions(this IMvcBuilder builder, Action<MvcXmlOptions> setupAction);
         public static IMvcBuilder AddXmlSerializerFormatters(this IMvcBuilder builder);
         public static IMvcBuilder AddXmlSerializerFormatters(this IMvcBuilder builder, Action<MvcXmlOptions> setupAction);
     }
     public static class MvcXmlMvcCoreBuilderExtensions {
         public static IMvcCoreBuilder AddXmlDataContractSerializerFormatters(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddXmlDataContractSerializerFormatters(this IMvcCoreBuilder builder, Action<MvcXmlOptions> setupAction);
         public static IMvcCoreBuilder AddXmlOptions(this IMvcCoreBuilder builder, Action<MvcXmlOptions> setupAction);
         public static IMvcCoreBuilder AddXmlSerializerFormatters(this IMvcCoreBuilder builder);
         public static IMvcCoreBuilder AddXmlSerializerFormatters(this IMvcCoreBuilder builder, Action<MvcXmlOptions> setupAction);
     }
-    public static class NodeServicesServiceCollectionExtensions {
 {
-        public static void AddNodeServices(this IServiceCollection serviceCollection);

-        public static void AddNodeServices(this IServiceCollection serviceCollection, Action<NodeServicesOptions> setupAction);

-    }
     public static class OAuthExtensions {
         public static AuthenticationBuilder AddOAuth(this AuthenticationBuilder builder, string authenticationScheme, Action<OAuthOptions> configureOptions);
         public static AuthenticationBuilder AddOAuth(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<OAuthOptions> configureOptions);
         public static AuthenticationBuilder AddOAuth<TOptions, THandler>(this AuthenticationBuilder builder, string authenticationScheme, Action<TOptions> configureOptions) where TOptions : OAuthOptions, new() where THandler : OAuthHandler<TOptions>;
         public static AuthenticationBuilder AddOAuth<TOptions, THandler>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TOptions> configureOptions) where TOptions : OAuthOptions, new() where THandler : OAuthHandler<TOptions>;
     }
     public class OAuthPostConfigureOptions<TOptions, THandler> : IPostConfigureOptions<TOptions> where TOptions : OAuthOptions, new() where THandler : OAuthHandler<TOptions> {
         public OAuthPostConfigureOptions(IDataProtectionProvider dataProtection);
         public void PostConfigure(string name, TOptions options);
     }
     public delegate object ObjectFactory(IServiceProvider serviceProvider, object[] arguments);
-    public static class OpenIdConnectExtensions {
 {
-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder);

-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, Action<OpenIdConnectOptions> configureOptions);

-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, Action<OpenIdConnectOptions> configureOptions);

-        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<OpenIdConnectOptions> configureOptions);

-    }
     public static class OptionsBuilderConfigurationExtensions {
         public static OptionsBuilder<TOptions> Bind<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config) where TOptions : class;
         public static OptionsBuilder<TOptions> Bind<TOptions>(this OptionsBuilder<TOptions> optionsBuilder, IConfiguration config, Action<BinderOptions> configureBinder) where TOptions : class;
     }
     public static class OptionsBuilderDataAnnotationsExtensions {
         public static OptionsBuilder<TOptions> ValidateDataAnnotations<TOptions>(this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class;
     }
     public static class OptionsConfigurationServiceCollectionExtensions {
         public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config) where TOptions : class;
         public static IServiceCollection Configure<TOptions>(this IServiceCollection services, IConfiguration config, Action<BinderOptions> configureBinder) where TOptions : class;
         public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, IConfiguration config) where TOptions : class;
         public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, IConfiguration config, Action<BinderOptions> configureBinder) where TOptions : class;
     }
     public static class OptionsServiceCollectionExtensions {
         public static IServiceCollection AddOptions(this IServiceCollection services);
         public static OptionsBuilder<TOptions> AddOptions<TOptions>(this IServiceCollection services) where TOptions : class;
         public static OptionsBuilder<TOptions> AddOptions<TOptions>(this IServiceCollection services, string name) where TOptions : class;
         public static IServiceCollection Configure<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class;
         public static IServiceCollection Configure<TOptions>(this IServiceCollection services, string name, Action<TOptions> configureOptions) where TOptions : class;
         public static IServiceCollection ConfigureAll<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class;
         public static IServiceCollection ConfigureOptions(this IServiceCollection services, object configureInstance);
         public static IServiceCollection ConfigureOptions(this IServiceCollection services, Type configureType);
         public static IServiceCollection ConfigureOptions<TConfigureOptions>(this IServiceCollection services) where TConfigureOptions : class;
         public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class;
         public static IServiceCollection PostConfigure<TOptions>(this IServiceCollection services, string name, Action<TOptions> configureOptions) where TOptions : class;
         public static IServiceCollection PostConfigureAll<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions) where TOptions : class;
     }
     public static class PageConventionCollectionExtensions {
         public static PageConventionCollection Add(this PageConventionCollection conventions, IParameterModelBaseConvention convention);
         public static PageConventionCollection AddAreaPageRoute(this PageConventionCollection conventions, string areaName, string pageName, string route);
         public static PageConventionCollection AddPageRoute(this PageConventionCollection conventions, string pageName, string route);
         public static PageConventionCollection AllowAnonymousToAreaFolder(this PageConventionCollection conventions, string areaName, string folderPath);
         public static PageConventionCollection AllowAnonymousToAreaPage(this PageConventionCollection conventions, string areaName, string pageName);
         public static PageConventionCollection AllowAnonymousToFolder(this PageConventionCollection conventions, string folderPath);
         public static PageConventionCollection AllowAnonymousToPage(this PageConventionCollection conventions, string pageName);
         public static PageConventionCollection AuthorizeAreaFolder(this PageConventionCollection conventions, string areaName, string folderPath);
         public static PageConventionCollection AuthorizeAreaFolder(this PageConventionCollection conventions, string areaName, string folderPath, string policy);
         public static PageConventionCollection AuthorizeAreaPage(this PageConventionCollection conventions, string areaName, string pageName);
         public static PageConventionCollection AuthorizeAreaPage(this PageConventionCollection conventions, string areaName, string pageName, string policy);
         public static PageConventionCollection AuthorizeFolder(this PageConventionCollection conventions, string folderPath);
         public static PageConventionCollection AuthorizeFolder(this PageConventionCollection conventions, string folderPath, string policy);
         public static PageConventionCollection AuthorizePage(this PageConventionCollection conventions, string pageName);
         public static PageConventionCollection AuthorizePage(this PageConventionCollection conventions, string pageName, string policy);
         public static PageConventionCollection ConfigureFilter(this PageConventionCollection conventions, IFilterMetadata filter);
         public static IPageApplicationModelConvention ConfigureFilter(this PageConventionCollection conventions, Func<PageApplicationModel, IFilterMetadata> factory);
     }
     public static class PolicyServiceCollectionExtensions {
+        public static IServiceCollection AddAuthorization(this IServiceCollection services);
+        public static IServiceCollection AddAuthorization(this IServiceCollection services, Action<AuthorizationOptions> configure);
         public static IServiceCollection AddAuthorizationPolicyEvaluator(this IServiceCollection services);
     }
-    public static class PrerenderingServiceCollectionExtensions {
 {
-        public static void AddSpaPrerenderer(this IServiceCollection serviceCollection);

-    }
     public static class ResponseCachingServicesExtensions {
         public static IServiceCollection AddResponseCaching(this IServiceCollection services);
         public static IServiceCollection AddResponseCaching(this IServiceCollection services, Action<ResponseCachingOptions> configureOptions);
     }
     public static class RoutingServiceCollectionExtensions {
         public static IServiceCollection AddRouting(this IServiceCollection services);
         public static IServiceCollection AddRouting(this IServiceCollection services, Action<RouteOptions> configureOptions);
     }
+    public static class ServerSideBlazorBuilderExtensions {
+        public static IServerSideBlazorBuilder AddCircuitOptions(this IServerSideBlazorBuilder builder, Action<CircuitOptions> configure);
+        public static IServerSideBlazorBuilder AddHubOptions(this IServerSideBlazorBuilder builder, Action<HubOptions> configure);
+    }
     public class ServiceCollection : ICollection<ServiceDescriptor>, IEnumerable, IEnumerable<ServiceDescriptor>, IList<ServiceDescriptor>, IServiceCollection {
         public ServiceCollection();
         public int Count { get; }
         public bool IsReadOnly { get; }
         public ServiceDescriptor this[int index] { get; set; }
         public void Clear();
         public bool Contains(ServiceDescriptor item);
         public void CopyTo(ServiceDescriptor[] array, int arrayIndex);
         public IEnumerator<ServiceDescriptor> GetEnumerator();
         public int IndexOf(ServiceDescriptor item);
         public void Insert(int index, ServiceDescriptor item);
         public bool Remove(ServiceDescriptor item);
         public void RemoveAt(int index);
         void System.Collections.Generic.ICollection<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>.Add(ServiceDescriptor item);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public static class ServiceCollectionContainerBuilderExtensions {
         public static ServiceProvider BuildServiceProvider(this IServiceCollection services);
         public static ServiceProvider BuildServiceProvider(this IServiceCollection services, ServiceProviderOptions options);
         public static ServiceProvider BuildServiceProvider(this IServiceCollection services, bool validateScopes);
     }
     public static class ServiceCollectionHostedServiceExtensions {
         public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services) where THostedService : class, IHostedService;
+        public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services, Func<IServiceProvider, THostedService> implementationFactory) where THostedService : class, IHostedService;
     }
     public static class ServiceCollectionServiceExtensions {
         public static IServiceCollection AddScoped(this IServiceCollection services, Type serviceType);
         public static IServiceCollection AddScoped(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
         public static IServiceCollection AddScoped(this IServiceCollection services, Type serviceType, Type implementationType);
         public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;
         public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
         public static IServiceCollection AddScoped<TService>(this IServiceCollection services) where TService : class;
         public static IServiceCollection AddScoped<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
         public static IServiceCollection AddSingleton(this IServiceCollection services, Type serviceType);
         public static IServiceCollection AddSingleton(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
         public static IServiceCollection AddSingleton(this IServiceCollection services, Type serviceType, object implementationInstance);
         public static IServiceCollection AddSingleton(this IServiceCollection services, Type serviceType, Type implementationType);
         public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;
         public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
         public static IServiceCollection AddSingleton<TService>(this IServiceCollection services) where TService : class;
         public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
         public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, TService implementationInstance) where TService : class;
         public static IServiceCollection AddTransient(this IServiceCollection services, Type serviceType);
         public static IServiceCollection AddTransient(this IServiceCollection services, Type serviceType, Func<IServiceProvider, object> implementationFactory);
         public static IServiceCollection AddTransient(this IServiceCollection services, Type serviceType, Type implementationType);
         public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services) where TService : class where TImplementation : class, TService;
         public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
         public static IServiceCollection AddTransient<TService>(this IServiceCollection services) where TService : class;
         public static IServiceCollection AddTransient<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory) where TService : class;
     }
     public class ServiceDescriptor {
         public ServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime);
         public ServiceDescriptor(Type serviceType, object instance);
         public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime);
         public Func<IServiceProvider, object> ImplementationFactory { get; }
         public object ImplementationInstance { get; }
         public Type ImplementationType { get; }
         public ServiceLifetime Lifetime { get; }
         public Type ServiceType { get; }
         public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime);
         public static ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime);
         public static ServiceDescriptor Scoped(Type service, Func<IServiceProvider, object> implementationFactory);
         public static ServiceDescriptor Scoped(Type service, Type implementationType);
         public static ServiceDescriptor Scoped<TService, TImplementation>() where TService : class where TImplementation : class, TService;
         public static ServiceDescriptor Scoped<TService, TImplementation>(Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
         public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class;
         public static ServiceDescriptor Singleton(Type serviceType, Func<IServiceProvider, object> implementationFactory);
         public static ServiceDescriptor Singleton(Type serviceType, object implementationInstance);
         public static ServiceDescriptor Singleton(Type service, Type implementationType);
         public static ServiceDescriptor Singleton<TService, TImplementation>() where TService : class where TImplementation : class, TService;
         public static ServiceDescriptor Singleton<TService, TImplementation>(Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
         public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class;
         public static ServiceDescriptor Singleton<TService>(TService implementationInstance) where TService : class;
+        public override string ToString();
         public static ServiceDescriptor Transient(Type service, Func<IServiceProvider, object> implementationFactory);
         public static ServiceDescriptor Transient(Type service, Type implementationType);
         public static ServiceDescriptor Transient<TService, TImplementation>() where TService : class where TImplementation : class, TService;
         public static ServiceDescriptor Transient<TService, TImplementation>(Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService;
         public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class;
     }
     public enum ServiceLifetime {
         Scoped = 1,
         Singleton = 0,
         Transient = 2,
     }
-    public sealed class ServiceProvider : IDisposable, IServiceProvider, IServiceProviderEngineCallback {
+    public sealed class ServiceProvider : IAsyncDisposable, IDisposable, IServiceProvider {
         public void Dispose();
+        public ValueTask DisposeAsync();
         public object GetService(Type serviceType);
     }
     public class ServiceProviderOptions {
         public ServiceProviderOptions();
+        public bool ValidateOnBuild { get; set; }
         public bool ValidateScopes { get; set; }
     }
     public static class ServiceProviderServiceExtensions {
         public static IServiceScope CreateScope(this IServiceProvider provider);
         public static object GetRequiredService(this IServiceProvider provider, Type serviceType);
         public static T GetRequiredService<T>(this IServiceProvider provider);
         public static T GetService<T>(this IServiceProvider provider);
         public static IEnumerable<object> GetServices(this IServiceProvider provider, Type serviceType);
         public static IEnumerable<T> GetServices<T>(this IServiceProvider provider);
     }
     public static class SessionServiceCollectionExtensions {
         public static IServiceCollection AddSession(this IServiceCollection services);
         public static IServiceCollection AddSession(this IServiceCollection services, Action<SessionOptions> configure);
     }
     public static class SignalRDependencyInjectionExtensions {
         public static ISignalRServerBuilder AddHubOptions<THub>(this ISignalRServerBuilder signalrBuilder, Action<HubOptions<THub>> configure) where THub : Hub;
         public static ISignalRServerBuilder AddSignalR(this IServiceCollection services);
         public static ISignalRServerBuilder AddSignalR(this IServiceCollection services, Action<HubOptions> configure);
         public static ISignalRServerBuilder AddSignalRCore(this IServiceCollection services);
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
     public static class TagHelperServicesExtensions {
         public static IMvcCoreBuilder AddCacheTagHelper(this IMvcCoreBuilder builder);
         public static IMvcBuilder AddCacheTagHelperLimits(this IMvcBuilder builder, Action<CacheTagHelperOptions> configure);
         public static IMvcCoreBuilder AddCacheTagHelperLimits(this IMvcCoreBuilder builder, Action<CacheTagHelperOptions> configure);
     }
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

