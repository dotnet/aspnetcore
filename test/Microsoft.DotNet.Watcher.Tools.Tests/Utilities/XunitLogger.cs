// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.Tests
{
    internal class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        public XunitLogger(ITestOutputHelper output)
        {
            _output = output;
        }

        public IDisposable BeginScope<TState>(TState state)
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _output.WriteLine($"{logLevel}: {formatter(state, exception)}");
        }

        private class NullScope : IDisposable
        {
            private NullScope() { }
            public static NullScope Instance = new NullScope();
            public void Dispose() { }
        }
    }
}