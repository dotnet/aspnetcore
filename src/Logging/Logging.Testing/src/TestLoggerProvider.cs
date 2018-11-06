// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Testing
{
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly ITestSink _sink;

        public TestLoggerProvider(ITestSink sink)
        {
            _sink = sink;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _sink, enabled: true);
        }

        public void Dispose()
        {
        }
    }
}
