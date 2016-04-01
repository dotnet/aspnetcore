// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.Elm
{
    public class ElmLogger : ILogger
    {
        private readonly string _name;
        private readonly ElmOptions _options;
        private readonly ElmStore _store;

        public ElmLogger(string name, ElmOptions options, ElmStore store)
        {
            _name = name;
            _options = options;
            _store = store;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, 
                          Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel) || (state == null && exception == null))
            {
                return;
            }
            LogInfo info = new LogInfo()
            {
                ActivityContext = GetCurrentActivityContext(),
                Name = _name,
                EventID = eventId.Id,
                Severity = logLevel,
                Exception = exception,
                State = state,
                Message = formatter == null ? state.ToString() : formatter(state, exception),
                Time = DateTimeOffset.UtcNow
            };
            if (ElmScope.Current != null)
            {
                ElmScope.Current.Node.Messages.Add(info);
            }
            // The log does not belong to any scope - create a new context for it
            else
            {
                var context = GetNewActivityContext();
                context.RepresentsScope = false;  // mark as a non-scope log
                context.Root = new ScopeNode();
                context.Root.Messages.Add(info);
                _store.AddActivity(context);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _options.Filter(_name, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var scope = new ElmScope(_name, state);
            scope.Context = ElmScope.Current?.Context ?? GetNewActivityContext();
            return ElmScope.Push(scope, _store);
        }

        private ActivityContext GetNewActivityContext()
        {
            return new ActivityContext()
            {
                Id = Guid.NewGuid(),
                Time = DateTimeOffset.UtcNow,
                RepresentsScope = true
            };
        }

        private ActivityContext GetCurrentActivityContext()
        {
            return ElmScope.Current?.Context ?? GetNewActivityContext();
        }
    }
}