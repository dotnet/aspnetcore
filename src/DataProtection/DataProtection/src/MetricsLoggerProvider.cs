// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.DataProtection;

internal sealed class MetricsLoggerProvider : ILoggerProvider
{
    public const string MeterName = "Microsoft.AspNetCore.DataProtection";
    private const string CategoryNamePrefix = "Microsoft.AspNetCore.DataProtection";

    private readonly Meter? _meter;

    private readonly ILogger _logger;

    public MetricsLoggerProvider()
    {
        _logger = NullLogger.Instance;
    }

    public MetricsLoggerProvider(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        var counter = _meter.CreateCounter<long>(
            "aspnetcore.dataprotection.log_messages",
            unit: "{message}",
            description: "Number of messages that have been logged.");

        _logger = new MetricsLogger(counter);
    }

    ILogger ILoggerProvider.CreateLogger(string categoryName) =>
        categoryName.StartsWith(CategoryNamePrefix, StringComparison.Ordinal)
            ? _logger
            : NullLogger.Instance;

    void IDisposable.Dispose() => _meter?.Dispose();

    private sealed class MetricsLogger : ILogger
    {
        private readonly Counter<long> _counter;

        public MetricsLogger(Counter<long> counter)
        {
            _counter = counter;
        }

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                case LogLevel.Warning:
                    return _counter.Enabled;
                default:
                    return false;
            }
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState _state, Exception? _exception, Func<TState, Exception?, string> _formatter)
        {
            var levelName = logLevel switch
            {
                LogLevel.Critical => "critical",
                LogLevel.Error => "error",
                LogLevel.Warning => "warning",
                _ => null,
            };

            if (levelName is null || !_counter.Enabled)
            {
                return;
            }

            var tags = new TagList(
            [
                new("id", eventId.Name ?? eventId.Id.ToString(CultureInfo.InvariantCulture)),
                new("level", levelName),
            ]);
            _counter.Add(1, tags);
        }
    }
}
