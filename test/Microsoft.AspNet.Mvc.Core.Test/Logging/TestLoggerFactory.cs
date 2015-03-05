// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc
{
    public class TestLoggerFactory : ILoggerFactory
    {
        private TestSink _sink;

        public TestLoggerFactory(TestSink sink)
        {
            _sink = sink;
        }

        public LogLevel MinimumLevel { get; set; }

        public ILogger CreateLogger(string name)
        {
            return new TestLogger(name, _sink);
        }

        public void AddProvider(ILoggerProvider provider)
        {

        }
    }
}