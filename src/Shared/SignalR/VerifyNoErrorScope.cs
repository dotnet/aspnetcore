// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class VerifyNoErrorsScope : IDisposable
    {
        private readonly IDisposable _wrappedDisposable;
        private readonly Func<WriteContext, bool> _expectedErrorsFilter;
        private readonly LogSinkProvider _sink;

        public ILoggerFactory LoggerFactory { get; }

        public VerifyNoErrorsScope(ILoggerFactory loggerFactory = null, IDisposable wrappedDisposable = null, Func<WriteContext, bool> expectedErrorsFilter = null)
        {
            _wrappedDisposable = wrappedDisposable;
            _expectedErrorsFilter = expectedErrorsFilter;
            _sink = new LogSinkProvider();

            LoggerFactory = loggerFactory ?? new LoggerFactory();
            LoggerFactory.AddProvider(_sink);
        }

        public void Dispose()
        {
            _wrappedDisposable?.Dispose();

            var results = _sink.GetLogs().Where(w => w.Write.LogLevel >= LogLevel.Error).ToList();

            if (_expectedErrorsFilter != null)
            {
                results = results.Where(w => !_expectedErrorsFilter(w.Write)).ToList();
            }

            if (results.Count > 0)
            {
                string errorMessage = $"{results.Count} error(s) logged.";
                errorMessage += Environment.NewLine;
                errorMessage += string.Join(Environment.NewLine, results.Select(record =>
                {
                    var r = record.Write;

                    string lineMessage = r.LoggerName + " - " + r.EventId.ToString() + " - " + r.Formatter(r.State, r.Exception);
                    if (r.Exception != null)
                    {
                        lineMessage += Environment.NewLine;
                        lineMessage += "===================";
                        lineMessage += Environment.NewLine;
                        lineMessage += r.Exception;
                        lineMessage += Environment.NewLine;
                        lineMessage += "===================";
                    }
                    return lineMessage;
                }));

                throw new Exception(errorMessage);
            }
        }
    }
}
