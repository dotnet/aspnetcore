// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class FixtureLoggedTest: LoggedTest
    {
        private readonly IISTestSiteFixture _fixture;

        public FixtureLoggedTest(IISTestSiteFixture fixture)
        {
            _fixture = fixture;
        }

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
            _fixture.Attach(this);
        }

        public override void Dispose()
        {
            _fixture.Detach(this);
            base.Dispose();
        }
    }
}
