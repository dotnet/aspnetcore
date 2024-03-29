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

    private readonly Meter? _meter;

    private readonly ILogger _logger;

    public MetricsLoggerProvider()
    {
        _logger = NullLogger.Instance;
    }

    public MetricsLoggerProvider(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        var errorCounter = _meter.CreateCounter<long>(
            "errors",
            unit: "{error}",
            description: "Number of errors that have occurred.");
        var warningCounter = _meter.CreateCounter<long>(
            "warnings",
            unit: "{warning}",
            description: "Number of warnings that have occurred.");

        _logger = new MetricsLogger(errorCounter, warningCounter);
    }

    ILogger ILoggerProvider.CreateLogger(string _categoryName) => _logger;

    void IDisposable.Dispose() => _meter?.Dispose();

    private sealed class MetricsLogger : ILogger
    {
        private readonly Counter<long> _errorCounter;
        private readonly Counter<long> _warningCounter;

        public MetricsLogger(Counter<long> errorCounter, Counter<long> warningCounter)
        {
            _errorCounter = errorCounter;
            _warningCounter = warningCounter;
        }

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        bool ILogger.IsEnabled(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Error => _errorCounter.Enabled,
            LogLevel.Warning => _warningCounter.Enabled,
            _ => false,
        };

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState _state, Exception? _exception, Func<TState, Exception?, string> _formatter)
        {
            switch(logLevel)
            {
                case LogLevel.Error:
                    if (_errorCounter.Enabled)
                    {
                        var tags = new TagList([new("id", eventId.Name ?? eventId.Id.ToString(CultureInfo.InvariantCulture))]);
                        _errorCounter.Add(1, tags);
                    }
                    break;
                case LogLevel.Warning:
                    if (_warningCounter.Enabled)
                    {
                        var tags = new TagList([new("id", eventId.Name ?? eventId.Id.ToString(CultureInfo.InvariantCulture))]);
                        _warningCounter.Add(1, tags);
                    }
                    break;
            }
        }
    }
}
