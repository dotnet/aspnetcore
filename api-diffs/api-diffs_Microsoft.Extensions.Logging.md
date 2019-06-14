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

     }
     public static class DebugLoggerFactoryExtensions {
-        public static ILoggerFactory AddDebug(this ILoggerFactory factory);

-        public static ILoggerFactory AddDebug(this ILoggerFactory factory, LogLevel minLevel);

-        public static ILoggerFactory AddDebug(this ILoggerFactory factory, Func<string, LogLevel, bool> filter);

     }
+    public static class EventLoggerFactoryExtensions {
+        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder);
+        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder, EventLogSettings settings);
+        public static ILoggingBuilder AddEventLog(this ILoggingBuilder builder, Action<EventLogSettings> configure);
+    }
     public static class EventSourceLoggerFactoryExtensions {
-        public static ILoggerFactory AddEventSourceLogger(this ILoggerFactory factory);

     }
     public class LoggerFactory : IDisposable, ILoggerFactory {
+        public static ILoggerFactory Create(Action<ILoggingBuilder> configure);
     }
     public class LoggerFilterOptions {
+        public bool CaptureScopes { get; set; }
     }
     public static class TraceSourceFactoryExtensions {
-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, SourceSwitch sourceSwitch);

-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, SourceSwitch sourceSwitch, TraceListener listener);

-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, string switchName);

-        public static ILoggerFactory AddTraceSource(this ILoggerFactory factory, string switchName, TraceListener listener);

     }
 }
```

