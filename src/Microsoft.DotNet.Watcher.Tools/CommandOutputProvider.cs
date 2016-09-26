// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watcher
{
    public class CommandOutputProvider : ILoggerProvider
    {
        private readonly bool _isWindows;

        public CommandOutputProvider()
        {
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
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
