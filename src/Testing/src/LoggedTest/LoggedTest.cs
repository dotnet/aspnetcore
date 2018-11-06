// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public class LoggedTest : LoggedTestBase
    {
        // Obsolete but keeping for back compat
        public LoggedTest(ITestOutputHelper output = null) : base (output) { }

        public ITestSink TestSink { get; set; }

        public override void Initialize(MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(methodInfo, testMethodArguments, testOutputHelper);

            TestSink = new TestSink();
            LoggerFactory.AddProvider(new TestLoggerProvider(TestSink));
        }
    }
}
