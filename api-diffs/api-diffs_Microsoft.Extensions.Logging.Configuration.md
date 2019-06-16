# Microsoft.Extensions.Logging.Configuration

``` diff
 namespace Microsoft.Extensions.Logging.Configuration {
     public interface ILoggerProviderConfiguration<T> {
         IConfiguration Configuration { get; }
     }
     public interface ILoggerProviderConfigurationFactory {
         IConfiguration GetConfiguration(Type providerType);
     }
+    public static class LoggerProviderOptions {
+        public static void RegisterProviderOptions<TOptions, TProvider>(IServiceCollection services) where TOptions : class;
+    }
     public class LoggerProviderOptionsChangeTokenSource<TOptions, TProvider> : ConfigurationChangeTokenSource<TOptions> {
         public LoggerProviderOptionsChangeTokenSource(ILoggerProviderConfiguration<TProvider> providerConfiguration);
     }
     public static class LoggingBuilderConfigurationExtensions {
         public static void AddConfiguration(this ILoggingBuilder builder);
     }
 }
```

