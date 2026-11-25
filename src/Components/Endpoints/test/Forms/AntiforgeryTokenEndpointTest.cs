// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.Forms;

public class AntiforgeryTokenEndpointTest
{
    [Fact]
    public async Task AcquiresUsableTokenAndReusesExistingCookie()
    {
        await using var services = CreateServices();
        var context = CreateContext(services);
        await AntiforgeryTokenEndpoint.WriteToken(context);

        Assert.Contains("no-store", context.Response.Headers.CacheControl.ToString());
        var cookie = Assert.Single(context.Response.Headers.SetCookie).Split(';')[0];
        using var json = JsonDocument.Parse(((MemoryStream)context.Response.Body).ToArray());
        var token = json.RootElement.GetProperty("value").GetString();
        var fieldName = json.RootElement.GetProperty("formFieldName").GetString();
        var post = new DefaultHttpContext { RequestServices = services };
        post.Request.Method = "POST";
        post.Request.Headers.Cookie = cookie;
        post.Request.ContentType = "application/x-www-form-urlencoded";
        post.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [fieldName] = token,
        });
        await services.GetRequiredService<IAntiforgery>().ValidateRequestAsync(post);

        var next = CreateContext(services);
        next.Request.Headers.Cookie = cookie;
        await AntiforgeryTokenEndpoint.WriteToken(next);
        Assert.Equal(0, next.Response.Headers.SetCookie.Count);
    }

    [Theory]
    [InlineData(false, true, StatusCodes.Status404NotFound)]
    [InlineData(true, false, StatusCodes.Status400BadRequest)]
    public async Task RejectsUnavailableOrInvalidAcquisition(bool middlewareInvoked, bool hasHeader, int expectedStatus)
    {
        await using var services = CreateServices();
        var context = CreateContext(services);
        if (!middlewareInvoked)
        {
            context.Items.Remove(MiddlewareInvokedKeys.Antiforgery);
        }
        if (!hasHeader)
        {
            context.Request.Headers.Remove("Blazor-Antiforgery");
        }

        await AntiforgeryTokenEndpoint.WriteToken(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public async Task DoesNotBypassCustomStateProvider()
    {
        var provider = new Mock<AntiforgeryStateProvider>(MockBehavior.Strict);
        await using var services = new ServiceCollection().AddLogging().AddAntiforgery()
            .AddSingleton(provider.Object).BuildServiceProvider();
        var context = CreateContext(services);

        await AntiforgeryTokenEndpoint.WriteToken(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
        provider.VerifyNoOtherCalls();
    }

    private static ServiceProvider CreateServices() =>
        new ServiceCollection().AddLogging().AddAntiforgery()
            .AddScoped<AntiforgeryStateProvider, EndpointAntiforgeryStateProvider>().BuildServiceProvider();

    private static DefaultHttpContext CreateContext(IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Items[MiddlewareInvokedKeys.Antiforgery] = MiddlewareInvokedKeys.Sentinel;
        context.Request.Headers["Blazor-Antiforgery"] = "true";
        context.Response.Body = new MemoryStream();

        return context;
    }
}
