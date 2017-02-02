using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging
{
    public static class XUnitLoggerFactoryExtensions
    {
        public static void AddXUnit(this ILoggerFactory self, ITestOutputHelper output)
        {
            self.AddProvider(new XUnitLoggerProvider(output));
        }
    }
}

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private ITestOutputHelper _output;

        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_output, categoryName, LogLevel.Trace);
        }

        public void Dispose()
        {
        }
    }

    public class XUnitLogger : ILogger, IDisposable
    {
        private readonly string _category;
        private readonly LogLevel _minLogLevel;
        private readonly ITestOutputHelper _output;
        private bool _disposed;

        public XUnitLogger(ITestOutputHelper output, string category, LogLevel minLogLevel)
        {
            _minLogLevel = minLogLevel;
            _category = category;
            _output = output;
        }

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var firstLinePrefix = $"| {_category} {logLevel}: ";
            var lines = formatter(state, exception).Split('\n');
            _output.WriteLine(firstLinePrefix + lines.First());

            var additionalLinePrefix = "|" + new string(' ', firstLinePrefix.Length - 1);
            foreach (var line in lines.Skip(1))
            {
                _output.WriteLine(additionalLinePrefix + line.Trim('\r'));
            }
        }

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= _minLogLevel && !_disposed;

        public IDisposable BeginScope<TState>(TState state)
            => new NullScope();

        private class NullScope : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
