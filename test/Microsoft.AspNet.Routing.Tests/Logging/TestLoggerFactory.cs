// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Routing
{
    public class TestLoggerFactory : ILoggerFactory
    {
        private TestSink _sink;
        private bool _enabled;

        public TestLoggerFactory(TestSink sink, bool enabled)
        {
            _sink = sink;
            _enabled = enabled;
        }

        public ILogger Create(string name)
        {
            return new TestLogger(name, _sink, _enabled);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}