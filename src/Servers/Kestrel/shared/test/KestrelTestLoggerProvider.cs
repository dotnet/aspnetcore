// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class KestrelTestLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _testLogger;

        public KestrelTestLoggerProvider(bool throwOnCriticalErrors = true)
            : this(new TestApplicationErrorLogger { ThrowOnCriticalErrors = throwOnCriticalErrors })
        {
        }

        public KestrelTestLoggerProvider(ILogger testLogger)
        {
            _testLogger = testLogger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _testLogger;
        }

        public void Dispose()
        {
        }
    }
}
