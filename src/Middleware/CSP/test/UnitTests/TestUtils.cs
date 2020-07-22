using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class TestUtils
    {
        public class CspTestLogger : ILogger<CspReportLogger>
        {
            public Dictionary<LogLevel, string> actualLogCalls = new Dictionary<LogLevel, string>();
            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                throw new NotImplementedException();
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                actualLogCalls.Add(logLevel, formatter.Invoke(state, exception));
            }

            public void SingleLogStatementMatching(LogLevel logLevel, string expected)
            {
                Assert.Single(actualLogCalls);

                string actualMessage;
                actualLogCalls.TryGetValue(logLevel, out actualMessage);
                Assert.Equal(expected, actualMessage);
            }

            public void NoLogStatementsMade()
            {
                Assert.Empty(actualLogCalls);
            }
        }
    }
}
