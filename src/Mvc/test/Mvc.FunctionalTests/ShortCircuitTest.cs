// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ShortCircuitTest
{
    [Theory]
    [InlineData(typeof(TestActionController), "/TestAction")]
    [InlineData(typeof(TestController), "/Test")]
    public async Task Works_on_Controller(Type controllerType, string requestUri)
    {
        var middlewareWasExecuted = false;

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMvcCore().UseSpecificControllers(controllerType);
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();

        app.UseRouting();

        app.Use(async (context, next) =>
        {
            middlewareWasExecuted = true;
            await next(context);
        });

        app.MapControllers();

        await app.StartAsync();

        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        var result = await client.SendAsync(request);
        result.EnsureSuccessStatusCode();

        Assert.False(middlewareWasExecuted);
    }

    [ApiController]
    public class TestActionController : ControllerBase
    {
        [Route("[controller]")]
        [ShortCircuit]
        public ActionResult Index() => new OkObjectResult(0);
    }

    [ApiController]
    [ShortCircuit]
    public class TestController : ControllerBase
    {
        [Route("[controller]")]
        public ActionResult Index() => new OkObjectResult(0);
    }
}
