// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class CommandOutputProvider : ILoggerProvider
    {
        private readonly bool _isWindows;

        public CommandOutputProvider()
        {
            _isWindows = PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Windows;
        }

        public ILogger CreateLogger(string name)
        {
            return new CommandOutputLogger(this, name, useConsoleColor: _isWindows);
        }

        public void Dispose()
        {
        }

        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}
