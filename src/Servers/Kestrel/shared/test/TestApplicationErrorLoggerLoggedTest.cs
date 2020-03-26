// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Testing
{
    public class TestApplicationErrorLoggerLoggedTest : LoggedTest
    {
        public TestApplicationErrorLogger TestApplicationErrorLogger { get; private set; }

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

            TestApplicationErrorLogger = new TestApplicationErrorLogger();
            LoggerFactory.AddProvider(new KestrelTestLoggerProvider(TestApplicationErrorLogger));
        }
    }
}
