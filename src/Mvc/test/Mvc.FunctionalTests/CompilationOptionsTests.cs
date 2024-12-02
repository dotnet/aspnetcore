// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

// Test to verify compilation options from the application are used to compile
// precompiled and dynamically compiled views.
public class CompilationOptionsTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<RazorWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task CompilationOptions_AreUsedByViewsAndPartials()
    {
        // Arrange
        var expected =
@"This method is running from NETCOREAPP2_0
This method is only defined in NETCOREAPP2_0";

        // Act
        var body = await Client.GetStringAsync("http://localhost/ViewsConsumingCompilationOptions/");

        // Assert
        Assert.Equal(expected, body.Trim(), ignoreLineEndingDifferences: true);
    }
}
