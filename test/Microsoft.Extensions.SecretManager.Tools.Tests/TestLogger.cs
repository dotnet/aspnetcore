// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests
{
    public class TestLogger : ILogger
    {
        private CommandOutputProvider _commandOutputProvider;
        private readonly ILogger _wrapped;
        private readonly ITestOutputHelper _output;

        public TestLogger(ITestOutputHelper output = null)
        {
            _commandOutputProvider = new CommandOutputProvider();
            _wrapped = _commandOutputProvider.CreateLogger("");
            _output = output;
        }

        public void SetLevel(LogLevel level)
        {
            _commandOutputProvider.LogLevel = LogLevel.Debug;
        }

        public List<string> Messages { get; set; } = new List<string>();

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _wrapped.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                Messages.Add(formatter(state, exception));
                _output?.WriteLine(formatter(state, exception));
            }
        }
    }
}