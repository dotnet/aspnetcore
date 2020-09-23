// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Testing
{
    public class LoggedTest : LoggedTestBase
    {
        // Obsolete but keeping for back compat
        public LoggedTest(ITestOutputHelper output = null) : base (output) { }

        public ITestSink TestSink { get; set; }

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

            TestSink = new TestSink();
            LoggerFactory.AddProvider(new TestLoggerProvider(TestSink));
        }
    }
}
