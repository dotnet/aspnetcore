// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class TagHelperLogger<T> : ILogger<T>
    {
        public List<LoggerData> Logged { get; } = new List<LoggerData>();

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logged.Add(new LoggerData(logLevel, state));
        }

        public class LoggerData
        {
            public LoggerData(LogLevel logLevel, object state)
            {
                LogLevel = logLevel;
                State = state;
            }

            public LogLevel LogLevel { get; set; }
            public object State { get; set; }
        }
    }
}