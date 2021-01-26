// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class FixtureLoggedTest : LoggedTest
    {
        protected IISTestSiteFixture Fixture { get; set; }

        public FixtureLoggedTest(IISTestSiteFixture fixture)
        {
            Fixture = fixture;
        }

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
            Fixture.Attach(this);
        }

        public override void Dispose()
        {
            Fixture.Detach(this);
            base.Dispose();
        }
    }
}
