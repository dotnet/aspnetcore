// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using RazorWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class DataAnnotationTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<StartupDataAnnotations>(LoggerFactory)
            .WithWebHostBuilder(builder =>
            {
                builder.UseStartup<StartupDataAnnotations>();
            });
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<StartupDataAnnotations> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    private const string EnumUrl = "http://localhost/Enum/Enum";

    [Fact]
    public async Task DataAnnotationLocalizationOfEnums_FromDataAnnotationLocalizerProvider()
    {
        // Arrange & Act
        var response = await Client.GetAsync(EnumUrl);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("FirstOptionDisplay from singletype", content);
    }
}
