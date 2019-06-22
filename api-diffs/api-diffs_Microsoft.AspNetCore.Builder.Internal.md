# Microsoft.AspNetCore.Builder.Internal

``` diff
-namespace Microsoft.AspNetCore.Builder.Internal {
 {
-    public class ApplicationBuilder : IApplicationBuilder {
 {
-        public ApplicationBuilder(IServiceProvider serviceProvider);

-        public ApplicationBuilder(IServiceProvider serviceProvider, object server);

-        public IServiceProvider ApplicationServices { get; set; }

-        public IDictionary<string, object> Properties { get; }

-        public IFeatureCollection ServerFeatures { get; }

-        public RequestDelegate Build();

-        public IApplicationBuilder New();

-        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);

-    }
-}
```

