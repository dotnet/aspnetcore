// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using BasicWebSite;
using BasicWebSite.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RazorPagesClassLibrary;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TestingInfrastructureTests : IClassFixture<WebApplicationFactory<BasicWebSite.StartupWithoutEndpointRouting>>
{
    public TestingInfrastructureTests(WebApplicationFactory<BasicWebSite.StartupWithoutEndpointRouting> fixture)
    {
        Factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(s => s.AddSingleton<TestService, OverridenService>());

    public WebApplicationFactory<StartupWithoutEndpointRouting> Factory { get; }
    public HttpClient Client { get; }

    [Fact]
    public async Task TestingInfrastructure_CanOverrideServiceFromWithinTheTest()
    {
        // Act
        var response = await Client.GetStringAsync("Testing/Builder");

        // Assert
        Assert.Equal("Test", response);
    }

    [Fact]
    public void TestingInfrastructure_CreateClientThrowsInvalidOperationForNonEntryPoint()
    {
        using var factory = new WebApplicationFactory<ClassLibraryStartup>();
        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Equal($"The provided Type '{typeof(RazorPagesClassLibrary.ClassLibraryStartup).Name}' does not belong to an assembly with an entry point. A common cause for this error is providing a Type from a class library.",
           ex.Message);
    }

    [Fact]
    public async Task TestingInfrastructure_RedirectHandlerWorksWithPreserveMethod()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "Testing/RedirectHandler/2")
        {
            Content = new ObjectContent<Number>(new Number { Value = 5 }, new JsonMediaTypeFormatter())
        };
        request.Headers.Add("X-Pass-Thru", "Some-Value");
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var xPassThruValue = Assert.Single(response.Headers.GetValues("X-Pass-Thru"));
        Assert.Equal("Some-Value", xPassThruValue);

        var handlerResponse = await response.Content.ReadAsAsync<RedirectHandlerResponse>();
        Assert.Equal(5, handlerResponse.Url);
        Assert.Equal(5, handlerResponse.Body);
    }

    [Fact]
    public async Task TestingInfrastructure_RedirectHandlerWorksWithInvalidRequestAndContentHeaders()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "Testing/RedirectHandler/2")
        {
            Content = new ObjectContent<Number>(new Number { Value = 5 }, new JsonMediaTypeFormatter())
        };
        request.Headers.Add("X-Pass-Thru", "Some-Value");
        Assert.True(request.Headers.TryAddWithoutValidation("X-Invalid-Request-Header", "Bearer 1234,5678"));
        Assert.True(request.Content.Headers.TryAddWithoutValidation("X-Invalid-Content-Header", "Bearer 1234,5678"));
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var xPassThruValue = Assert.Single(response.Headers.GetValues("X-Pass-Thru"));
        Assert.Equal("Some-Value", xPassThruValue);

        var handlerResponse = await response.Content.ReadAsAsync<RedirectHandlerResponse>();
        Assert.Equal(5, handlerResponse.Url);
        Assert.Equal(5, handlerResponse.Body);
    }

    [Fact]
    public async Task TestingInfrastructure_RedirectHandlerUsesOriginalRequestHeaders()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "Testing/RedirectHandler/Headers");
        var client = Factory.CreateDefaultClient(
            new RedirectHandler(), new TestHandler());
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var modifiedHeaderWasSent = await response.Content.ReadAsStringAsync();

        Assert.Equal("false", modifiedHeaderWasSent);
    }

    [Fact]
    public async Task TestingInfrastructure_RedirectHandler_SupportsZeroMaxRedirects()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "Testing/RedirectHandler/Redirect303");
        var client = Factory.CreateDefaultClient(
            new RedirectHandler(0), new TestHandler());
        var response = await client.SendAsync(request);

        // Assert that we don't follow the redirect because MaxRedirects = 0
        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal("/Testing/Builder", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task TestingInfrastructure_RedirectHandlerHandlesRelativeLocation()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "Testing/RedirectHandler/Relative/");
        var client = Factory.CreateDefaultClient(
            new RedirectHandler());
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TestingInfrastructure_RedirectHandlerFollowsStatusCode303()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "Testing/RedirectHandler/Redirect303");
        var client = Factory.CreateDefaultClient(
            new RedirectHandler());
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Test", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestingInfrastructure_PostRedirectGetWorksWithCookies()
    {
        // Act
        var acquireToken = await Client.GetAsync("Testing/AntiforgerySimulator/3");
        Assert.Equal(HttpStatusCode.OK, acquireToken.StatusCode);

        var response = await Client.PostAsync(
            "Testing/PostRedirectGet/Post/3",
            content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var handlerResponse = await response.Content.ReadAsAsync<PostRedirectGetGetResponse>();
        Assert.Equal(4, handlerResponse.TempDataValue);
        Assert.Equal("Value-4", handlerResponse.CookieValue);
    }

    [Fact]
    public async Task TestingInfrastructure_PutWithoutBodyFollowsRedirects()
    {
        // Act
        var response = await Client.PutAsync("Testing/Put/3", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, await response.Content.ReadAsAsync<int>());
    }

    [Fact]
    public async Task TestingInfrastructure_WorksWithGenericHost()
    {
        using var factory = new WebApplicationFactory<GenericHostWebSite.Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(s => s.AddSingleton<GenericHostWebSite.TestGenericService, OverridenGenericService>()));

        var response = await factory.CreateClient().GetStringAsync("Testing/Builder");

        Assert.Equal("GenericTest", response);
    }

    [Fact]
    public void TestingInfrastructure_HasServicesUsingWebHostProgram()
    {
        using var factory = new WebApplicationFactory<BasicWebSite.Program>();

        Assert.NotNull(factory.Services);
        Assert.NotNull(factory.Services.GetService(typeof(IConfiguration)));
    }

    [Fact]
    public void TestingInfrastructure_HasServicesUsingWebHostStartup()
    {
        using var factory = new WebApplicationFactory<BasicWebSite.Startup>();

        Assert.NotNull(factory.Services);
        Assert.NotNull(factory.Services.GetService(typeof(IConfiguration)));
    }

    [Fact]
    public void TestingInfrastructure_HasServicesUsingGenericHostProgram()
    {
        using var factory = new WebApplicationFactory<GenericHostWebSite.Program>();

        Assert.NotNull(factory.Services);
        Assert.NotNull(factory.Services.GetService(typeof(IConfiguration)));
    }

    [Fact]
    public void TestingInfrastructure_HasServicesUsingGenericHostStartup()
    {
        using var factory = new WebApplicationFactory<GenericHostWebSite.Startup>();

        Assert.NotNull(factory.Services);
        Assert.NotNull(factory.Services.GetService(typeof(IConfiguration)));
    }

    [Fact]
    public async Task TestingInfrastructure_RedirectHandlerDoesNotCopyAuthorizationHeaders()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "Testing/RedirectHandler/RedirectToAuthorized");
        var client = Factory.CreateDefaultClient(
            new RedirectHandler(), new TestHandler());
        request.Headers.Add("Authorization", "Bearer key");
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class OverridenService : TestService
    {
        public OverridenService()
        {
            Message = "Test";
        }
    }

    private class OverridenGenericService : GenericHostWebSite.TestGenericService
    {
        public OverridenGenericService()
        {
            Message = "GenericTest";
        }
    }

    private class TestHandler : DelegatingHandler
    {
        public TestHandler()
        {
        }

        public TestHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        public bool HeaderAdded { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!HeaderAdded)
            {
                request.Headers.Add("X-Added-Header", "true");
                HeaderAdded = true;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
