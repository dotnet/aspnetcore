// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ClientValidationOptionsTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorPagesWebSite.Startup>(LoggerFactory);
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<RazorPagesWebSite.Startup> Factory { get; private set; }

    [Fact]
    public async Task DisablingClientValidation_DisablesItForPagesAndViews()
    {
        // Arrange
        var client = Factory
            .WithWebHostBuilder(whb => whb.UseStartup<RazorPagesWebSite.StartupWithClientValidationDisabled>())
            .CreateClient();

        // Act
        var view = await client.GetStringAsync("Controller/ClientValidationDisabled");
        var page = await client.GetStringAsync("ClientvalidationDisabled");

        // Assert
        Assert.Equal("ClientValidationDisabled", view);
        Assert.Equal("ClientValidationDisabled", page);
    }
}
