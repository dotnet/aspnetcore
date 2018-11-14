// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Testing
{
    public class TestApplicationErrorLoggerLoggedTest : LoggedTest
    {
        public TestApplicationErrorLogger TestApplicationErrorLogger { get; private set; }

        public override void AdditionalSetup()
        {
            TestApplicationErrorLogger = new TestApplicationErrorLogger();
            LoggerFactory.AddProvider(new KestrelTestLoggerProvider(TestApplicationErrorLogger));
        }
    }
}
