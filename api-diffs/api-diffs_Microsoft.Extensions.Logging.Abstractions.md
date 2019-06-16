# Microsoft.Extensions.Logging.Abstractions

``` diff
 namespace Microsoft.Extensions.Logging.Abstractions {
     public class NullLogger : ILogger {
         public static NullLogger Instance { get; }
         public IDisposable BeginScope<TState>(TState state);
         public bool IsEnabled(LogLevel logLevel);
         public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
     }
     public class NullLogger<T> : ILogger, ILogger<T> {
         public static readonly NullLogger<T> Instance;
         public NullLogger();
         public IDisposable BeginScope<TState>(TState state);
         public bool IsEnabled(LogLevel logLevel);
         public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);
     }
     public class NullLoggerFactory : IDisposable, ILoggerFactory {
         public static readonly NullLoggerFactory Instance;
         public NullLoggerFactory();
         public void AddProvider(ILoggerProvider provider);
         public ILogger CreateLogger(string name);
         public void Dispose();
     }
     public class NullLoggerProvider : IDisposable, ILoggerProvider {
         public static NullLoggerProvider Instance { get; }
         public ILogger CreateLogger(string categoryName);
         public void Dispose();
     }
 }
```

