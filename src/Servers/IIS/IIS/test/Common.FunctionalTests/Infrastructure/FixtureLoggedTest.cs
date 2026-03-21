// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public class FixtureLoggedTest : LoggedTest
{
    protected IISTestSiteFixture Fixture { get; set; }

    public FixtureLoggedTest(IISTestSiteFixture fixture)
    {
        Fixture = fixture;
    }

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
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
