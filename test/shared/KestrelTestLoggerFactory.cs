// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class KestrelTestLoggerFactory : ILoggerFactory
    {
        private readonly ILogger _testLogger;

        public KestrelTestLoggerFactory()
            : this(new TestApplicationErrorLogger())
        {
        }

        public KestrelTestLoggerFactory(ILogger testLogger)
        {
            _testLogger = testLogger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _testLogger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}