// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SimpleWithWebApplicationBuilderTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.Program>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.Program> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task HelloWorld()
    {
        // Arrange
        var expected = "Hello World";

        // Act
        var content = await Client.GetStringAsync("http://localhost/");

        // Assert
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task JsonResult_Works()
    {
        // Arrange
        var expected = "{\"name\":\"John\",\"age\":42}";

        // Act
        var response = await Client.GetAsync("/json");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task OkObjectResult_Works()
    {
        // Arrange
        var expected = "{\"name\":\"John\",\"age\":42}";

        // Act
        var response = await Client.GetAsync("/ok-object");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task AcceptedObjectResult_Works()
    {
        // Arrange
        var expected = "{\"name\":\"John\",\"age\":42}";

        // Act
        var response = await Client.GetAsync("/accepted-object");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.Accepted);
        Assert.Equal("/ok-object", response.Headers.Location.ToString());
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task ActionReturningMoreThanOneResult_NotFound()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/many-results?id=-1");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActionReturningMoreThanOneResult_Found()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/many-results?id=7");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.MovedPermanently);
        Assert.Equal("/json", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task MvcControllerActionWorks()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/greet");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello human", content);
    }

    [Fact]
    public async Task DefaultEnvironment_Is_Development()
    {
        // Arrange
        var expected = "Development";
        using var client = new WebApplicationFactory<SimpleWebSiteWithWebApplicationBuilder.Program>().CreateClient();

        // Act
        var content = await client.GetStringAsync("http://localhost/environment");

        // Assert
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task Configuration_Can_Be_Overridden()
    {
        // Arrange
        var fixture = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(builder =>
            {
                var config = new[]
                {
                        KeyValuePair.Create("Greeting", "Bonjour tout le monde"),
                };

                builder.AddInMemoryCollection(config);
            });
        });

        var expected = "Bonjour tout le monde";
        using var client = fixture.CreateDefaultClient();

        // Act
        var content = await client.GetStringAsync("http://localhost/greeting");

        // Assert
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task Environment_Can_Be_Overridden()
    {
        // Arrange
        var fixture = Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Staging);
        });

        var expected = "Staging";
        using var client = fixture.CreateDefaultClient();

        // Act
        var content = await client.GetStringAsync("http://localhost/environment");

        // Assert
        Assert.Equal(expected, content);
    }

    [Fact]
    public async Task WebRoot_Can_Be_Overriden()
    {
        var webRoot = "foo";
        var expectedWebRoot = "";
        // Arrange
        var fixture = Factory.WithWebHostBuilder(builder =>
        {
            expectedWebRoot = Path.GetFullPath(Path.Combine(builder.GetSetting(WebHostDefaults.ContentRootKey), webRoot));
            builder.UseSetting(WebHostDefaults.WebRootKey, webRoot);
        });

        using var client = fixture.CreateDefaultClient();

        // Act
        var content = await client.GetStringAsync("http://localhost/webroot");

        // Assert
        Assert.Equal(expectedWebRoot, content);
    }

    [Fact]
    public async Task Accepts_Json_WhenBindingAComplexType()
    {
        // Act
        var response = await Client.PostAsJsonAsync("accepts-default", new { name = "Test" });

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Rejects_NonJson_WhenBindingAComplexType()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "accepts-default");
        request.Content = new StringContent("<xml />");
        request.Content.Headers.ContentType = new("application/xml");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Accepts_NonJsonMediaType()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "accepts-xml");
        request.Content = new StringContent("<xml />");
        request.Content.Headers.ContentType = new("application/xml");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task FileUpload_Works_WithAntiforgeryToken()
    {
        // Arrange
        var expected = "42";
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(new string('a', 42)), "file", "file.txt");

        var antiforgery = Factory.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = Factory.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        Client.DefaultRequestHeaders.Add("Cookie", new CookieHeaderValue(antiforgeryOptions.Value.Cookie.Name, tokens.CookieToken).ToString());
        Client.DefaultRequestHeaders.Add(tokens.HeaderName, tokens.RequestToken);

        // Act
        var response = await Client.PostAsync("/fileupload", content);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var actual = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task FileUpload_Fails_WithoutAntiforgeryToken()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(new string('a', 42)), "file", "file.txt");

        // Act
        var response = await Client.PostAsync("/fileupload", content);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }
}
