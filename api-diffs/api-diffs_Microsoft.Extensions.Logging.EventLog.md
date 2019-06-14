# Microsoft.Extensions.Logging.EventLog

``` diff
+namespace Microsoft.Extensions.Logging.EventLog {
+    public class EventLogLoggerProvider : IDisposable, ILoggerProvider, ISupportExternalScope {
+        public EventLogLoggerProvider();
+        public EventLogLoggerProvider(EventLogSettings settings);
+        public EventLogLoggerProvider(IOptions<EventLogSettings> options);
+        public ILogger CreateLogger(string name);
+        public void Dispose();
+        public void SetScopeProvider(IExternalScopeProvider scopeProvider);
+    }
+    public class EventLogSettings {
+        public EventLogSettings();
+        public Func<string, LogLevel, bool> Filter { get; set; }
+        public string LogName { get; set; }
+        public string MachineName { get; set; }
+        public string SourceName { get; set; }
+    }
+}
```

