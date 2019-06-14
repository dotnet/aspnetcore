# Microsoft.Extensions.Logging.Console

``` diff
 namespace Microsoft.Extensions.Logging.Console {
-    public class ConfigurationConsoleLoggerSettings : IConsoleLoggerSettings {
 {
-        public ConfigurationConsoleLoggerSettings(IConfiguration configuration);

-        public IChangeToken ChangeToken { get; private set; }

-        public bool IncludeScopes { get; }

-        public IConsoleLoggerSettings Reload();

-        public bool TryGetSwitch(string name, out LogLevel level);

-    }
-    public class ConsoleLogger : ILogger {
 {
-        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider);

-        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes);

-        public IConsole Console { get; set; }

-        public bool DisableColors { get; set; }

-        public Func<string, LogLevel, bool> Filter { get; set; }

-        public bool IncludeScopes { get; set; }

-        public string Name { get; }

-        public IDisposable BeginScope<TState>(TState state);

-        public bool IsEnabled(LogLevel logLevel);

-        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

-        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception);

-    }
+    public enum ConsoleLoggerFormat {
+        Default = 0,
+        Systemd = 1,
+    }
     public class ConsoleLoggerOptions {
+        public ConsoleLoggerFormat Format { get; set; }
+        public LogLevel LogToStandardErrorThreshold { get; set; }
+        public string TimestampFormat { get; set; }
     }
     public class ConsoleLoggerProvider : IDisposable, ILoggerProvider, ISupportExternalScope {
-        public ConsoleLoggerProvider(IConsoleLoggerSettings settings);

-        public ConsoleLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes);

-        public ConsoleLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes, bool disableColors);

     }
-    public class ConsoleLoggerSettings : IConsoleLoggerSettings {
 {
-        public ConsoleLoggerSettings();

-        public IChangeToken ChangeToken { get; set; }

-        public bool DisableColors { get; set; }

-        public bool IncludeScopes { get; set; }

-        public IDictionary<string, LogLevel> Switches { get; set; }

-        public IConsoleLoggerSettings Reload();

-        public bool TryGetSwitch(string name, out LogLevel level);

-    }
-    public class ConsoleLogScope {
 {
-        public static ConsoleLogScope Current { get; set; }

-        public ConsoleLogScope Parent { get; private set; }

-        public static IDisposable Push(string name, object state);

-        public override string ToString();

-    }
-    public interface IConsoleLoggerSettings {
 {
-        IChangeToken ChangeToken { get; }

-        bool IncludeScopes { get; }

-        IConsoleLoggerSettings Reload();

-        bool TryGetSwitch(string name, out LogLevel level);

-    }
 }
```

