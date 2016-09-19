// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.SecretManager.Tools
{
    /// <summary>
    /// Logger to print formatted command output.
    /// </summary>
    public class CommandOutputLogger : ILogger
    {
        private readonly CommandOutputProvider _provider;
        private readonly AnsiConsole _outConsole;

        public CommandOutputLogger(CommandOutputProvider commandOutputProvider, bool useConsoleColor)
        {
            _provider = commandOutputProvider;
            _outConsole = AnsiConsole.GetOutput(useConsoleColor);
        }

        public IDisposable BeginScope<TState>(TState state)
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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _outConsole.WriteLine(string.Format("{0}: {1}", Caption(logLevel), formatter(state, exception)));
            }
        }

        private string Caption(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace: return "\x1b[35mtrace\x1b[39m";
                case LogLevel.Debug: return "\x1b[35mdebug\x1b[39m";
                case LogLevel.Information: return "\x1b[32minfo\x1b[39m";
                case LogLevel.Warning: return "\x1b[33mwarn\x1b[39m";
                case LogLevel.Error: return "\x1b[31mfail\x1b[39m";
                case LogLevel.Critical: return "\x1b[31mcritical\x1b[39m";
            }

            throw new Exception("Unknown LogLevel");
        }
    }
}