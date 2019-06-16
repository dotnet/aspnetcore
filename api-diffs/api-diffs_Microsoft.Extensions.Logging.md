# Microsoft.Extensions.Logging

``` diff
 namespace Microsoft.Extensions.Logging {
     public static class ConsoleLoggerExtensions {
-        public static ILoggerFactory AddConsole(this ILoggerFactory factory);

-        public static ILoggerFactory AddConsole(this ILoggerFactory factory, IConfiguration configuration);

-        public static ILoggerFactory AddConsole(this ILoggerFactory factory, IConsoleLoggerSettings settings);

-        public static ILoggerFactory AddConsole(this ILoggerFactory factory, LogLevel minLevel);

-        public static ILoggerFactory AddConsole(this ILoggerFactory factory, LogLevel minLevel, bool includeScopes);

-        public static ILoggerFactory AddConsole(this ILoggerFactory factory, bool includeScopes);

-        public static ILoggerFactory AddConsole(this ILoggerFactory factory, Func<string, LogLevel, bool> filter);

-        public static ILoggerFactory AddConsole(this ILoggerFactory factory, Func<string, LogLevel, bool> filter, bool includeScopes);

         public static ILoggingBuilder AddConsole(this ILoggingBuilder builder);
         public static ILoggingBuilder AddConsole(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> configure);
     }
     public static class DebugLoggerFactoryExtensions {
-        public static ILoggerFactory AddDebug(this ILoggerFactory factory);

-        public static ILoggerFactory AddDebug(this ILoggerFactory factory, LogLevel minLevel);

-        public static ILoggerFactory AddDebug(this ILoggerFactory factory, Func<string, LogLevel, bool> filter);

         public static ILoggingBuilder AddDebug(this ILoggingBuilder builder);
     }
     public readonly struct EventId {
         public EventId(int id, string name = null);
         public int Id { get; }
         public string Name { get; }
         public bool Equals(EventId other);
         public override bool Equals(object obj);
         public override int GetHashCode();
         public static bool operator ==(EventId left, EventId right);
         public static implicit operator EventId (int i);
         public static bool operator !=(EventId left, EventId right);
         public override string ToString();
     }
+    public static class EventLoggerFactoryExtensions {
+        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder);
+        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder, EventLogSettings settings);
+        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder, Action<EventLogSettings> configure);
+    }
     public static class EventSourceLoggerFactoryExtensions {
-        public static ILoggerFactory AddEventSourceLogger(this ILoggerFactory factory);

         public static ILoggingBuilder AddEventSourceLogger(this ILoggingBuilder builder);
     }
     public static class FilterLoggingBuilderExtensions {
         public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<LogLevel, bool> levelFilter);
         public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string, LogLevel, bool> categoryLevelFilter);
         public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string, string, LogLevel, bool> filter);
         public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string category, LogLevel level);
         public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string category, Func<LogLevel, bool> levelFilter);
         public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<LogLevel, bool> levelFilter);
         public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<string, LogLevel, bool> categoryLevelFilter);
         public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, Func<string, string, LogLevel, bool> filter);
         public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, string category, LogLevel level);
         public static LoggerFilterOptions AddFilter(this LoggerFilterOptions builder, string category, Func<LogLevel, bool> levelFilter);
         public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider;
         public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<string, LogLevel, bool> categoryLevelFilter) where T : ILoggerProvider;
         public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string category, LogLevel level) where T : ILoggerProvider;
         public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string category, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider;
         public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider;
         public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, Func<string, LogLevel, bool> categoryLevelFilter) where T : ILoggerProvider;
         public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, string category, LogLevel level) where T : ILoggerProvider;
         public static LoggerFilterOptions AddFilter<T>(this LoggerFilterOptions builder, string category, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider;
     }
     public interface IExternalScopeProvider {
         void ForEachScope<TState>(Action<object, TState> callback, TState state);
         IDisposable Push(object state);
     }
     public interface ILogger {
         IDisposable BeginScope<TState>(TState state);
         bool IsEnabled(LogLevel logLevel);
         void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
     }
     public interface ILogger<out TCategoryName> : ILogger
     public interface ILoggerFactory : IDisposable {
         void AddProvider(ILoggerProvider provider);
         ILogger CreateLogger(string categoryName);
     }
     public interface ILoggerProvider : IDisposable {
         ILogger CreateLogger(string categoryName);
     }
     public interface ILoggingBuilder {
         IServiceCollection Services { get; }
     }
     public interface ISupportExternalScope {
         void SetScopeProvider(IExternalScopeProvider scopeProvider);
     }
     public class Logger<T> : ILogger, ILogger<T> {
         public Logger(ILoggerFactory factory);
         IDisposable Microsoft.Extensions.Logging.ILogger.BeginScope<TState>(TState state);
         bool Microsoft.Extensions.Logging.ILogger.IsEnabled(LogLevel logLevel);
         void Microsoft.Extensions.Logging.ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
     }
     public static class LoggerExtensions {
         public static IDisposable BeginScope(this ILogger logger, string messageFormat, params object[] args);
         public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args);
         public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, params object[] args);
         public static void Log(this ILogger logger, LogLevel logLevel, Exception exception, string message, params object[] args);
         public static void Log(this ILogger logger, LogLevel logLevel, string message, params object[] args);
         public static void LogCritical(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args);
         public static void LogCritical(this ILogger logger, EventId eventId, string message, params object[] args);
         public static void LogCritical(this ILogger logger, Exception exception, string message, params object[] args);
         public static void LogCritical(this ILogger logger, string message, params object[] args);
         public static void LogDebug(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args);
         public static void LogDebug(this ILogger logger, EventId eventId, string message, params object[] args);
         public static void LogDebug(this ILogger logger, Exception exception, string message, params object[] args);
         public static void LogDebug(this ILogger logger, string message, params object[] args);
         public static void LogError(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args);
         public static void LogError(this ILogger logger, EventId eventId, string message, params object[] args);
         public static void LogError(this ILogger logger, Exception exception, string message, params object[] args);
         public static void LogError(this ILogger logger, string message, params object[] args);
         public static void LogInformation(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args);
         public static void LogInformation(this ILogger logger, EventId eventId, string message, params object[] args);
         public static void LogInformation(this ILogger logger, Exception exception, string message, params object[] args);
         public static void LogInformation(this ILogger logger, string message, params object[] args);
         public static void LogTrace(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args);
         public static void LogTrace(this ILogger logger, EventId eventId, string message, params object[] args);
         public static void LogTrace(this ILogger logger, Exception exception, string message, params object[] args);
         public static void LogTrace(this ILogger logger, string message, params object[] args);
         public static void LogWarning(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args);
         public static void LogWarning(this ILogger logger, EventId eventId, string message, params object[] args);
         public static void LogWarning(this ILogger logger, Exception exception, string message, params object[] args);
         public static void LogWarning(this ILogger logger, string message, params object[] args);
     }
     public class LoggerExternalScopeProvider : IExternalScopeProvider {
         public LoggerExternalScopeProvider();
         public void ForEachScope<TState>(Action<object, TState> callback, TState state);
         public IDisposable Push(object state);
     }
     public class LoggerFactory : IDisposable, ILoggerFactory {
         public LoggerFactory();
         public LoggerFactory(IEnumerable<ILoggerProvider> providers);
         public LoggerFactory(IEnumerable<ILoggerProvider> providers, LoggerFilterOptions filterOptions);
         public LoggerFactory(IEnumerable<ILoggerProvider> providers, IOptionsMonitor<LoggerFilterOptions> filterOption);
         public void AddProvider(ILoggerProvider provider);
         protected virtual bool CheckDisposed();
+        public static ILoggerFactory Create(Action<ILoggingBuilder> configure);
         public ILogger CreateLogger(string categoryName);
         public void Dispose();
     }
     public static class LoggerFactoryExtensions {
         public static ILogger CreateLogger(this ILoggerFactory factory, Type type);
         public static ILogger<T> CreateLogger<T>(this ILoggerFactory factory);
     }
     public class LoggerFilterOptions {
         public LoggerFilterOptions();
+        public bool CaptureScopes { get; set; }
         public LogLevel MinLevel { get; set; }
         public IList<LoggerFilterRule> Rules { get; }
     }
     public class LoggerFilterRule {
         public LoggerFilterRule(string providerName, string categoryName, Nullable<LogLevel> logLevel, Func<string, string, LogLevel, bool> filter);
         public string CategoryName { get; }
         public Func<string, string, LogLevel, bool> Filter { get; }
         public Nullable<LogLevel> LogLevel { get; }
         public string ProviderName { get; }
         public override string ToString();
     }
     public static class LoggerMessage {
         public static Action<ILogger, Exception> Define(LogLevel logLevel, EventId eventId, string formatString);
         public static Action<ILogger, T1, T2, T3, T4, T5, T6, Exception> Define<T1, T2, T3, T4, T5, T6>(LogLevel logLevel, EventId eventId, string formatString);
         public static Action<ILogger, T1, T2, T3, T4, T5, Exception> Define<T1, T2, T3, T4, T5>(LogLevel logLevel, EventId eventId, string formatString);
         public static Action<ILogger, T1, T2, T3, T4, Exception> Define<T1, T2, T3, T4>(LogLevel logLevel, EventId eventId, string formatString);
         public static Action<ILogger, T1, T2, T3, Exception> Define<T1, T2, T3>(LogLevel logLevel, EventId eventId, string formatString);
         public static Action<ILogger, T1, T2, Exception> Define<T1, T2>(LogLevel logLevel, EventId eventId, string formatString);
         public static Action<ILogger, T1, Exception> Define<T1>(LogLevel logLevel, EventId eventId, string formatString);
         public static Func<ILogger, IDisposable> DefineScope(string formatString);
         public static Func<ILogger, T1, T2, T3, IDisposable> DefineScope<T1, T2, T3>(string formatString);
         public static Func<ILogger, T1, T2, IDisposable> DefineScope<T1, T2>(string formatString);
         public static Func<ILogger, T1, IDisposable> DefineScope<T1>(string formatString);
     }
     public static class LoggingBuilderExtensions {
         public static ILoggingBuilder AddConfiguration(this ILoggingBuilder builder, IConfiguration configuration);
         public static ILoggingBuilder AddProvider(this ILoggingBuilder builder, ILoggerProvider provider);
         public static ILoggingBuilder ClearProviders(this ILoggingBuilder builder);
         public static ILoggingBuilder SetMinimumLevel(this ILoggingBuilder builder, LogLevel level);
     }
     public enum LogLevel {
         Critical = 5,
         Debug = 1,
         Error = 4,
         Information = 2,
         None = 6,
         Trace = 0,
         Warning = 3,
     }
     public class ProviderAliasAttribute : Attribute {
         public ProviderAliasAttribute(string alias);
         public string Alias { get; }
     }
     public static class TraceSourceFactoryExtensions {
-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, SourceSwitch sourceSwitch);

-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, SourceSwitch sourceSwitch, TraceListener listener);

-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, string switchName);

-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, string switchName, TraceListener listener);

         public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder, SourceSwitch sourceSwitch);
         public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder, SourceSwitch sourceSwitch, TraceListener listener);
         public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder, string switchName);
         public static ILoggingBuilder AddTraceSource(this ILoggingBuilder builder, string switchName, TraceListener listener);
     }
 }
```

