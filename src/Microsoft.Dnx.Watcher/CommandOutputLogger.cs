// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.Framework.Logging;

namespace Microsoft.Dnx.Watcher
{
    /// <summary>
    /// Logger to print formatted command output.
    /// </summary>
    public class CommandOutputLogger : ILogger
    {
        private readonly CommandOutputProvider _provider;
        private readonly AnsiConsole _outConsole;
        private readonly string _loggerName;

        public CommandOutputLogger(CommandOutputProvider commandOutputProvider, string loggerName, bool useConsoleColor)
        {
            _provider = commandOutputProvider;
            _outConsole = AnsiConsole.GetOutput(useConsoleColor);
            _loggerName = loggerName;
        }

        public IDisposable BeginScopeImpl(object state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel < _provider.LogLevel)
            {
                return false;
            }

            return true;
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _outConsole.WriteLine($"[{_loggerName}] {Caption(logLevel)}: {formatter(state, exception)}");
            }
        }

        private string Caption(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug: return "\x1b[35mdebug\x1b[39m";
                case LogLevel.Verbose: return "\x1b[35mverbose\x1b[39m";
                case LogLevel.Information: return "\x1b[32minfo\x1b[39m";
                case LogLevel.Warning: return "\x1b[33mwarn\x1b[39m";
                case LogLevel.Error: return "\x1b[31mfail\x1b[39m";
                case LogLevel.Critical: return "\x1b[31mcritical\x1b[39m";
            }

            throw new Exception("Unknown LogLevel");
        }
    }
}
