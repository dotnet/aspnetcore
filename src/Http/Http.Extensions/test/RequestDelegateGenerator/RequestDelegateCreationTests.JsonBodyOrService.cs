// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    public static object[][] MapAction_JsonBodyOrService_SimpleReturn_Data
    {
        get
        {
            var todo = new Todo()
            {
                Id = 0,
                Name = "Test Item",
                IsComplete = false
            };
            var expectedTodoBody = "Test Item";
            var expectedServiceBody = "Produced from service!";
            var implicitRequiredServiceSource = $"""app.MapPost("/", ({typeof(TestService)} svc) => svc.TestServiceMethod());""";
            var implicitRequiredJsonBodySource = $"""app.MapPost("/", (Todo todo) => todo.Name ?? string.Empty);""";
            var implicitRequiredJsonBodyViaAsParametersSource = $"""app.MapPost("/", ([AsParameters] ParametersListWithImplicitFromBody args) => args.Todo.Name ?? string.Empty);""";

            return new[]
            {
                new object[] { implicitRequiredServiceSource, false, null, true, 200, expectedServiceBody },
                new object[] { implicitRequiredServiceSource, false, null, false, 400, string.Empty },
                new object[] { implicitRequiredJsonBodySource, true, todo, false, 200, expectedTodoBody },
                new object[] { implicitRequiredJsonBodySource, true, null, false, 400, string.Empty },
                new object[] { implicitRequiredJsonBodyViaAsParametersSource, true, todo, false, 200, expectedTodoBody },
                new object[] { implicitRequiredJsonBodyViaAsParametersSource, true, null, false, 400, string.Empty },
            };
        }
    }

    [Theory]
    [MemberData(nameof(MapAction_JsonBodyOrService_SimpleReturn_Data))]
    public async Task MapAction_JsonBodyOrService_SimpleReturn(string source, bool hasBody, Todo requestData, bool hasService, int expectedStatusCode, string expectedBody)
    {
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider(hasService ?
            (serviceCollection) => serviceCollection.AddSingleton(new TestService())
            : null);
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContext(serviceProvider);

        if (hasBody)
        {
            httpContext = CreateHttpContextWithBody(requestData);
        }

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody, expectedStatusCode);
    }

    [Fact]
    public async Task MapAction_JsonBodyOrService_HandlesBothJsonAndService()
    {
        var source = """
app.MapPost("/", (Todo todo, TestService svc) => $"{svc.TestServiceMethod()}, {todo.Name ?? string.Empty}");
""";
        var expectedBody = "Produced from service!, Test";
        var requestData = new Todo
        {
            Id = 1,
            Name = "Test",
            IsComplete = false
        };
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider((serviceCollection) => serviceCollection.AddSingleton(new TestService()));
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var httpContext = CreateHttpContextWithBody(requestData, serviceProvider);

        await VerifyAgainstBaselineUsingFile(compilation);

        await endpoint.RequestDelegate(httpContext);
        await VerifyResponseBodyAsync(httpContext, expectedBody);
    }

        public static IEnumerable<object[]> BodyParamOptionalityData
    {
        get
        {
            return new List<object[]>
            {
                new object[] { @"(Todo todo) => $""Todo: {todo.Name}"";", false, true, null },
                new object[] { @"(Todo todo) => $""Todo: {todo.Name}"";", true, false, "Todo: Default Todo"},
                new object[] { @"(Todo? todo = null) => $""Todo: {todo?.Name}"";", false, false, "Todo: "},
                new object[] { @"(Todo? todo = null) => $""Todo: {todo?.Name}"";", true, false, "Todo: Default Todo"},
                new object[] { @"(Todo? todo) => $""Todo: {todo?.Name}"";", false, false, "Todo: " },
                new object[] { @"(Todo? todo) => $""Todo: {todo?.Name}"";", true, false, "Todo: Default Todo" },
            };
        }
    }

    [Theory]
    [MemberData(nameof(BodyParamOptionalityData))]
    public async Task RequestDelegateHandlesBodyParamOptionality(string innerSource, bool hasBody, bool isInvalid, string expectedBody)
    {
        var source = $"""
string handler{innerSource};
app.MapPost("/", handler);
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var serviceProvider = CreateServiceProvider();
        var endpoint = GetEndpointFromCompilation(compilation, serviceProvider: serviceProvider);

        var todo = new Todo() { Name = "Default Todo" };
        var httpContext = hasBody ? CreateHttpContextWithBody(todo) : CreateHttpContextWithBody(null);

        await endpoint.RequestDelegate(httpContext);

        if (isInvalid)
        {
            var logs = TestSink.Writes.ToArray();
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(logs);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(5, "ImplicitBodyNotProvided"), log.EventId);
            Assert.Equal(@"Implicit body inferred for parameter ""todo"" but no body was provided. Did you mean to use a Service instead?", log.Message);
        }
        else
        {
            await VerifyResponseBodyAsync(httpContext, expectedBody);
        }
    }
}
