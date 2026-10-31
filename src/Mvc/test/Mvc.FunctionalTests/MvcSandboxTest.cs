// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class MvcSandboxTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<MvcSandbox.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<MvcSandbox.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task Home_Pages_ReturnSuccess()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RazorPages_ReturnSuccess()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/PagesHome");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }
}
