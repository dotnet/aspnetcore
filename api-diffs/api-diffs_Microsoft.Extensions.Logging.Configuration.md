# Microsoft.Extensions.Logging.Configuration

``` diff
 namespace Microsoft.Extensions.Logging.Configuration {
+    public static class LoggerProviderOptions {
+        public static void RegisterProviderOptions<TOptions, TProvider>(IServiceCollection services) where TOptions : class;
+    }
 }
```

