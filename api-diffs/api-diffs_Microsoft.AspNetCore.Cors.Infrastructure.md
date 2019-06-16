# Microsoft.AspNetCore.Cors.Infrastructure

``` diff
 namespace Microsoft.AspNetCore.Cors.Infrastructure {
     public static class CorsConstants {
         public static readonly string AccessControlAllowCredentials;
         public static readonly string AccessControlAllowHeaders;
         public static readonly string AccessControlAllowMethods;
         public static readonly string AccessControlAllowOrigin;
         public static readonly string AccessControlExposeHeaders;
         public static readonly string AccessControlMaxAge;
         public static readonly string AccessControlRequestHeaders;
         public static readonly string AccessControlRequestMethod;
         public static readonly string AnyOrigin;
         public static readonly string Origin;
         public static readonly string PreflightHttpMethod;
     }
     public class CorsMiddleware {
-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, CorsPolicy policy);

         public CorsMiddleware(RequestDelegate next, ICorsService corsService, CorsPolicy policy, ILoggerFactory loggerFactory);
-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider);

-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider, ILoggerFactory loggerFactory);

-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider, ILoggerFactory loggerFactory, string policyName);

-        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ICorsPolicyProvider policyProvider, string policyName);

+        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ILoggerFactory loggerFactory);
+        public CorsMiddleware(RequestDelegate next, ICorsService corsService, ILoggerFactory loggerFactory, string policyName);
-        public Task Invoke(HttpContext context);

+        public Task Invoke(HttpContext context, ICorsPolicyProvider corsPolicyProvider);
     }
     public class CorsOptions {
         public CorsOptions();
         public string DefaultPolicyName { get; set; }
         public void AddDefaultPolicy(CorsPolicy policy);
         public void AddDefaultPolicy(Action<CorsPolicyBuilder> configurePolicy);
         public void AddPolicy(string name, CorsPolicy policy);
         public void AddPolicy(string name, Action<CorsPolicyBuilder> configurePolicy);
         public CorsPolicy GetPolicy(string name);
     }
     public class CorsPolicy {
         public CorsPolicy();
         public bool AllowAnyHeader { get; }
         public bool AllowAnyMethod { get; }
         public bool AllowAnyOrigin { get; }
         public IList<string> ExposedHeaders { get; }
         public IList<string> Headers { get; }
         public Func<string, bool> IsOriginAllowed { get; set; }
         public IList<string> Methods { get; }
         public IList<string> Origins { get; }
         public Nullable<TimeSpan> PreflightMaxAge { get; set; }
         public bool SupportsCredentials { get; set; }
         public override string ToString();
     }
     public class CorsPolicyBuilder {
         public CorsPolicyBuilder(CorsPolicy policy);
         public CorsPolicyBuilder(params string[] origins);
         public CorsPolicyBuilder AllowAnyHeader();
         public CorsPolicyBuilder AllowAnyMethod();
         public CorsPolicyBuilder AllowAnyOrigin();
         public CorsPolicyBuilder AllowCredentials();
         public CorsPolicy Build();
         public CorsPolicyBuilder DisallowCredentials();
         public CorsPolicyBuilder SetIsOriginAllowed(Func<string, bool> isOriginAllowed);
         public CorsPolicyBuilder SetIsOriginAllowedToAllowWildcardSubdomains();
         public CorsPolicyBuilder SetPreflightMaxAge(TimeSpan preflightMaxAge);
         public CorsPolicyBuilder WithExposedHeaders(params string[] exposedHeaders);
         public CorsPolicyBuilder WithHeaders(params string[] headers);
         public CorsPolicyBuilder WithMethods(params string[] methods);
         public CorsPolicyBuilder WithOrigins(params string[] origins);
     }
     public class CorsResult {
         public CorsResult();
         public IList<string> AllowedExposedHeaders { get; }
         public IList<string> AllowedHeaders { get; }
         public IList<string> AllowedMethods { get; }
         public string AllowedOrigin { get; set; }
         public bool IsOriginAllowed { get; set; }
         public bool IsPreflightRequest { get; set; }
         public Nullable<TimeSpan> PreflightMaxAge { get; set; }
         public bool SupportsCredentials { get; set; }
         public bool VaryByOrigin { get; set; }
         public override string ToString();
     }
     public class CorsService : ICorsService {
-        public CorsService(IOptions<CorsOptions> options);

         public CorsService(IOptions<CorsOptions> options, ILoggerFactory loggerFactory);
         public virtual void ApplyResult(CorsResult result, HttpResponse response);
         public CorsResult EvaluatePolicy(HttpContext context, CorsPolicy policy);
         public CorsResult EvaluatePolicy(HttpContext context, string policyName);
         public virtual void EvaluatePreflightRequest(HttpContext context, CorsPolicy policy, CorsResult result);
         public virtual void EvaluateRequest(HttpContext context, CorsPolicy policy, CorsResult result);
     }
     public class DefaultCorsPolicyProvider : ICorsPolicyProvider {
         public DefaultCorsPolicyProvider(IOptions<CorsOptions> options);
         public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName);
     }
+    public interface ICorsMetadata
+    public interface ICorsPolicyMetadata : ICorsMetadata {
+        CorsPolicy Policy { get; }
+    }
     public interface ICorsPolicyProvider {
         Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName);
     }
     public interface ICorsService {
         void ApplyResult(CorsResult result, HttpResponse response);
         CorsResult EvaluatePolicy(HttpContext context, CorsPolicy policy);
     }
-    public interface IDisableCorsAttribute
+    public interface IDisableCorsAttribute : ICorsMetadata
-    public interface IEnableCorsAttribute {
+    public interface IEnableCorsAttribute : ICorsMetadata {
         string PolicyName { get; set; }
     }
 }
```

