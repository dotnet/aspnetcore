# Microsoft.Extensions.Logging.Debug

``` diff
 namespace Microsoft.Extensions.Logging.Debug {
-    public class DebugLogger : ILogger {
 {
-        public DebugLogger(string name);

-        public DebugLogger(string name, Func<string, LogLevel, bool> filter);

-        public IDisposable BeginScope<TState>(TState state);

-        public bool IsEnabled(LogLevel logLevel);

-        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

-    }
     public class DebugLoggerProvider : IDisposable, ILoggerProvider {
-        public DebugLoggerProvider(Func<string, LogLevel, bool> filter);

     }
 }
```

