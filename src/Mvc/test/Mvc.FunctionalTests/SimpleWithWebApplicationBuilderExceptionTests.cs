// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SimpleWithWebApplicationBuilderExceptionTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.Program>(LoggerFactory);
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.Program> Factory { get; private set; }

    [Fact]
    public void ExceptionThrownFromApplicationCanBeObserved()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Factory.CreateClient());
        Assert.Equal("This application failed to start", ex.Message);
    }
}
