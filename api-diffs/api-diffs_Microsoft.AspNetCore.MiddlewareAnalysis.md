# Microsoft.AspNetCore.MiddlewareAnalysis

``` diff
-namespace Microsoft.AspNetCore.MiddlewareAnalysis {
 {
-    public class AnalysisBuilder : IApplicationBuilder {
 {
-        public AnalysisBuilder(IApplicationBuilder inner);

-        public IServiceProvider ApplicationServices { get; set; }

-        public IDictionary<string, object> Properties { get; }

-        public IFeatureCollection ServerFeatures { get; }

-        public RequestDelegate Build();

-        public IApplicationBuilder New();

-        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);

-    }
-    public class AnalysisMiddleware {
 {
-        public AnalysisMiddleware(RequestDelegate next, DiagnosticSource diagnosticSource, string middlewareName);

-        public Task Invoke(HttpContext httpContext);

-    }
-    public class AnalysisStartupFilter : IStartupFilter {
 {
-        public AnalysisStartupFilter();

-        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next);

-    }
-}
```

