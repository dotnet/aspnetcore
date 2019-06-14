# Microsoft.Extensions.Logging.EventSource

``` diff
+namespace Microsoft.Extensions.Logging.EventSource {
+    public class EventSourceLoggerProvider : IDisposable, ILoggerProvider {
+        public EventSourceLoggerProvider(LoggingEventSource eventSource);
+        public ILogger CreateLogger(string categoryName);
+        public void Dispose();
+    }
+    public sealed class LoggingEventSource : EventSource {
+        protected override void OnEventCommand(EventCommandEventArgs command);
+        public static class Keywords {
+            public const EventKeywords FormattedMessage = (long)4;
+            public const EventKeywords JsonMessage = (long)8;
+            public const EventKeywords Message = (long)2;
+            public const EventKeywords Meta = (long)1;
+        }
+    }
+}
```

