// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ViewComponentFromServicesTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<ControllersFromServicesWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<ControllersFromServicesWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task ViewComponentsWithConstructorInjectionAreCreatedAndActivated()
    {
        // Arrange
        var expected = "Value = 3";
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/another/InServicesViewComponent");

        // Act
        var response = await Client.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(expected, responseText);
    }
}
