// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SimpleTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<SimpleWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<SimpleWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task JsonSerializeFormatted()
    {
        // Arrange
        var expected = "{" + Environment.NewLine
             + "  \"first\": \"wall\"," + Environment.NewLine
             + "  \"second\": \"floor\"" + Environment.NewLine
             + "}";

        // Act
        var content = await Client.GetStringAsync("http://localhost/Home/Index");

        // Assert
        Assert.Equal(expected, content);
    }
}
