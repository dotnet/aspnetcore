// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.SecretManager.Tools
{
    public class CommandOutputProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string name)
        {
            var useConsoleColor = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return new CommandOutputLogger(this, useConsoleColor);
        }

        public void Dispose()
        {
        }

        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}