// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using BasicWebSite.Models;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class HttpActionResultTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupWithSystemTextJson>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<BasicWebSite.StartupWithSystemTextJson> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task ActionCanReturnIResultWithContent()
    {
        // Arrange
        var id = 1;
        var url = $"/contact/{nameof(BasicWebSite.ContactApiController.ActionReturningObjectIResult)}/{id}";
        var response = await Client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Contact>();
        Assert.NotNull(result);
        Assert.Equal(id, result.ContactId);
    }

    [Fact]
    public async Task ActionCanReturnIResultWithStatusCodeOnly()
    {
        // Arrange
        var url = $"/contact/{nameof(BasicWebSite.ContactApiController.ActionReturningStatusCodeIResult)}";
        var response = await Client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.NoContent);
    }
}
