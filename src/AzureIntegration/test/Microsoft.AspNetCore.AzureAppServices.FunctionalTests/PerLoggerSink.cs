using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    class PerLoggerSink : ILogEventSink, IDisposable
    {
        readonly Action<string, LoggerSinkConfiguration> _configure;
        readonly object _sync = new object();
        readonly Dictionary<string, Logger> _sinkMap = new Dictionary<string, Logger>();

        public PerLoggerSink(Action<string, LoggerSinkConfiguration> configure)
        {
            _configure = configure;
        }

        public void Emit(LogEvent logEvent)
        {
            var key = "Main";
            if (logEvent.Properties.TryGetValue("TestName", out var value) &&
                value is ScalarValue scalarValue)
            {
                key = scalarValue.Value.ToString();
            }

            Logger sink;
            lock (_sync)
            {
                if (!_sinkMap.TryGetValue(key, out sink))
                {
                    var config = new LoggerConfiguration()
                        .MinimumLevel.Is(LevelAlias.Minimum);

                    _configure(key, config.WriteTo);
                    sink = _sinkMap[key] = config.CreateLogger();
                }
            }

            // Outside the lock to improve concurrency; this means the sink
            // may throw ObjectDisposedException, which is fine.
            sink.Write(logEvent);
        }

        public void Dispose()
        {
            lock (_sync)
            {
                var values = _sinkMap.Values;
                _sinkMap.Clear();
                foreach (var sink in values)
                {
                    sink.Dispose();
                }
            }
        }
    }
}