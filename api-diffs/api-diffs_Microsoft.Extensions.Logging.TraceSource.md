# Microsoft.Extensions.Logging.TraceSource

``` diff
 namespace Microsoft.Extensions.Logging.TraceSource {
-    public class TraceSourceLogger : ILogger {
 {
-        public TraceSourceLogger(TraceSource traceSource);

-        public IDisposable BeginScope<TState>(TState state);

-        public bool IsEnabled(LogLevel logLevel);

-        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

-    }
-    public class TraceSourceScope : IDisposable {
 {
-        public TraceSourceScope(object state);

-        public void Dispose();

-    }
 }
```

