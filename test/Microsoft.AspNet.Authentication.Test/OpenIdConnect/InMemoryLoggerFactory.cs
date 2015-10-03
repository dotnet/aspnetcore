// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    public class InMemoryLoggerFactory : ILoggerFactory
    {
        InMemoryLogger _logger;
        LogLevel _logLevel = LogLevel.Debug;

        public InMemoryLoggerFactory(LogLevel logLevel)
        {
            _logLevel = logLevel;
            _logger = new InMemoryLogger(_logLevel);
        }

        public LogLevel MinimumLevel
        {
            get { return _logLevel; }
            set { _logLevel = value; }
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void Dispose()
        {
        }

        public InMemoryLogger Logger { get { return _logger; } }
    }
}
