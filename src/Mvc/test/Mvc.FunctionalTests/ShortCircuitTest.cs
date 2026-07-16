// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ShortCircuitTest
{
    [Theory]
    [InlineData(typeof(ShortCircuitToMethodController))]
    [InlineData(typeof(ShortCircuitToMethodReturns400Controller))]
    [InlineData(typeof(ShortCircuitToMethodClassController))]
    [InlineData(typeof(ShortCircuitToClassReturns400Controller))]
    public async Task Works_WithShortCircuit_MiddlewareWasNotExecuted(Type controllerType)
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

        var request = new HttpRequestMessage(HttpMethod.Get, "Test/OkResult");
        await client.SendAsync(request);
        request = new HttpRequestMessage(HttpMethod.Get, "Test/ObjectResult");
        await client.SendAsync(request);

        Assert.False(middlewareWasExecuted);
    }

    [Theory]
    [InlineData(typeof(ShortCircuitToMethodController), HttpStatusCode.OK)]
    [InlineData(typeof(ShortCircuitToMethodReturns400Controller), HttpStatusCode.BadRequest)]
    [InlineData(typeof(ShortCircuitToMethodClassController), HttpStatusCode.OK)]
    [InlineData(typeof(ShortCircuitToClassReturns400Controller), HttpStatusCode.BadRequest)]
    public async Task Works_WithShortCircuit_ResponseCodeCheck(Type controllerType, HttpStatusCode objectResultStatusCode)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMvcCore().UseSpecificControllers(controllerType);
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.UseRouting();
        app.MapControllers();
        await app.StartAsync();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "Test/OkResult");
        var okResult = await client.SendAsync(request);
        request = new HttpRequestMessage(HttpMethod.Get, "Test/ObjectResult");
        var objectResult = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, okResult.StatusCode);
        Assert.Equal(objectResultStatusCode, objectResult.StatusCode);
    }

    [ApiController]
    [Route("Test/[action]")]
    private class ShortCircuitToMethodController : ControllerBase
    {
        [ShortCircuit]
        public ActionResult OkResult() => new OkObjectResult(0);

        [ShortCircuit]
        public object ObjectResult() => new();
    }

    [ApiController]
    [Route("Test/[action]")]
    private class ShortCircuitToMethodReturns400Controller : ControllerBase
    {
        [ShortCircuit(400)]
        public ActionResult OkResult() => new OkObjectResult(0);

        [ShortCircuit(400)]
        public object ObjectResult() => new();
    }

    [ApiController]
    [Route("Test/[action]")]
    [ShortCircuit]
    private class ShortCircuitToMethodClassController : ControllerBase
    {
        public ActionResult OkResult() => new OkObjectResult(0);

        public object ObjectResult() => new();
    }

    [ApiController]
    [Route("Test/[action]")]
    [ShortCircuit(400)]
    private class ShortCircuitToClassReturns400Controller : ControllerBase
    {
        public ActionResult OkResult() => new OkObjectResult(0);

        public object ObjectResult() => new();
    }
}
