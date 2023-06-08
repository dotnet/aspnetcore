// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Generators.Tests;

public abstract partial class RequestDelegateCreationTests : RequestDelegateCreationTestBase
{
    [Fact]
    public async Task RequestDelegateInvokesFiltersButNotHandler_OnArgumentError()
    {
        var source = """
app.MapGet("/", (HttpContext httpContext, string name) =>
{
    httpContext.Items["invoked"] = true;
    return $"Hello, {name}!";
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    context.HttpContext.Items["filterInvoked"] = true;
    context.Arguments[1] = context.Arguments[1] != null ? $"{((string)context.Arguments[1]!)}Prefix" : "NULL";
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        await endpoint.RequestDelegate(httpContext);

        // Assert
        Assert.Null(httpContext.Items["invoked"]);
        Assert.True(httpContext.Items["filterInvoked"] as bool?);
        await VerifyResponseBodyAsync(httpContext, string.Empty, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task RequestDelegateInvokesFilters_OnDelegateWithTarget()
    {
        // Arrange
        var source = """
app.MapGet("/", (HttpContext httpContext, string name) =>
{
    httpContext.Items["invoked"] = true;
    return $"Hello, {name}!";
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    context.HttpContext.Items["filterInvoked"] = true;
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "TestName"
        });

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        Assert.True(httpContext.Items["invoked"] as bool?);
        Assert.True(httpContext.Items["filterInvoked"] as bool?);
        await VerifyResponseBodyAsync(httpContext, "Hello, TestName!");
    }

    [Fact]
    public async Task RequestDelegateCanInvokeSingleEndpointFilter_ThatProvidesCustomErrorMessage()
    {
        // Arrange
        var source = """
app.MapGet("/", (HttpContext httpContext, string name) =>
{
    httpContext.Items["invoked"] = true;
    return $"Hello, {name}!";
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    if (context.HttpContext.Response.StatusCode == 400)
    {
        return Results.Problem("New response", statusCode: 400);
    }
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseJsonBodyAsync<ProblemDetails>(httpContext, (problemDetails) =>
        {
            Assert.Equal("New response", problemDetails.Detail);
        }, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task RequestDelegateCanInvokeMultipleEndpointFilters_ThatTouchArguments()
    {
        // Arrange
        var source = """
void Log(HttpContext context, string arg)
{
    int loggerInvoked = (int?)context.Items["loggerInvoked"] ?? 0;
    context.Items["loggerInvoked"] = loggerInvoked + 1;
}
app.MapGet("/", (string name, int age) =>
{
    return $"Hello, {name}! You are {age} years old.";
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    context.Arguments[1] = ((int)context.Arguments[1]!) + 2;
    return await next(context);
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    foreach (var parameter in context.Arguments)
    {
        Log(context.HttpContext, parameter!.ToString() ?? "no arg");
    }
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "TestName",
            ["age"] = "25"
        });

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseBodyAsync(httpContext, "Hello, TestName! You are 27 years old.");
        Assert.Equal(2, httpContext.Items["loggerInvoked"]);
    }

    [Fact]
    public async Task RequestDelegateCanInvokeEndpointFilter_ThatUsesMethodInfo()
    {
        // Arrange
        var source = """
app.MapGet("/", (string name) =>
{
    return $"Hello, {name}!";
})
.AddEndpointFilterFactory((routeHandlerContext, next) =>
{
    var parameters = routeHandlerContext.MethodInfo.GetParameters();
    var isInt = parameters.Length == 2 && parameters[1].ParameterType == typeof(int);
    return async (context) =>
    {
        if (isInt)
        {
            context.Arguments[1] = ((int)context.Arguments[1]!) + 2;
            return await next(context);
        }
        return "Is not an int.";
    };
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "TestName"
        });

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseBodyAsync(httpContext, "Is not an int.");
    }

    [Fact]
    public async Task RequestDelegateCanInvokeEndpointFilter_ThatReadsEndpointMetadata()
    {
        // Arrange
        var source = """
app.MapGet("/", (IFormFileCollection formFiles) =>
{
    return $"Got {formFiles.Count} files.";
})
.AddEndpointFilterFactory((routeHandlerContext, next) =>
{
    string? contentType = null;

    return async (context) =>
    {
        contentType ??= context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes.SingleOrDefault();

        if (contentType == "multipart/form-data")
        {
            return "I see you expect a form.";
        }

        return await next(context);
    };
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        // Act
        httpContext.Features.Set<IEndpointFeature>(new EndpointFeature { Endpoint = endpoint });

        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseBodyAsync(httpContext, "I see you expect a form.");
    }

    [Fact]
    public async Task RequestDelegateCanInvokeSingleEndpointFilter_ThatModifiesBodyParameter()
    {
        // Arrange
        var source = """
string PrintTodo(Todo todo)
{
    return $"{todo.Name} is {(todo.IsComplete ? "done" : "not done")}.";
};
app.MapPost("/", PrintTodo)
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    Todo originalTodo = (Todo)context.Arguments[0]!;
    originalTodo!.IsComplete = !originalTodo.IsComplete;
    context.Arguments[0] = originalTodo;
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContextWithBody(new Todo { Name = "Write tests", IsComplete = true });

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseBodyAsync(httpContext, "Write tests is not done.");
    }

    [Fact]
    public async Task RequestDelegateCanInvokeSingleEndpointFilter_ThatModifiesResult()
    {
        // Arrange
        var source = """
app.MapPost("/", (string name) =>
{
    return $"Hello, {name}!";
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    var previousResult = await next(context);
    if (previousResult is string stringResult)
    {
        return stringResult.ToUpperInvariant();
    }
    return previousResult;
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "TestName"
        });

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseBodyAsync(httpContext, "HELLO, TESTNAME!");
    }

    [Fact]
    public async Task RequestDelegateCanInvokeMultipleEndpointFilters_ThatModifyArgumentsAndResult()
    {
        // Arrange
        var source = """
app.MapPost("/", (string name) =>
{
    return $"Hello, {name}!";
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
   var previousResult = await next(context);
   if (previousResult is string stringResult)
   {
       return stringResult.ToUpperInvariant();
   }
   return previousResult;
})
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    var newValue = $"{context.GetArgument<string>(0)}Prefix";
    context.Arguments[0] = newValue;
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "TestName"
        });

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseBodyAsync(httpContext, "HELLO, TESTNAMEPREFIX!");
    }

    public static object[][] TaskOfTMethods
    {
        get
        {
            var taskOfTMethod = """
Task<string> TestAction()
{
    return Task.FromResult("foo");
}
""";
            var taskOfTWithYieldMethod = """
async Task<string> TestAction()
{
    await Task.Yield();
    return "foo";
}
""";
            var taskOfObjectWithYieldMethod = """
async Task<object> TestAction()
{
    await Task.Yield();
    return "foo";
}
""";

            return new object[][]
            {
                new object[] { taskOfTMethod },
                new object[] { taskOfTWithYieldMethod },
                new object[] { taskOfObjectWithYieldMethod }
            };
        }
    }

    [Theory]
    [MemberData(nameof(TaskOfTMethods))]
    public async Task CanInvokeFilter_OnTaskOfTReturningHandler(string innerSource)
    {
        // Arrange
        var source = $$"""
{{innerSource}}
app.MapGet("/", TestAction)
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        // Act
        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, "foo");
    }

    public static object[][] ValueTaskOfTMethods
    {
        get
        {
            var taskOfTMethod = """
ValueTask<string> TestAction()
{
    return ValueTask.FromResult("foo");
}
""";
            var taskOfTWithYieldMethod = """
async ValueTask<string> TestAction()
{
    await Task.Yield();
    return "foo";
}
""";
            var taskOfObjectWithYieldMethod = """
async ValueTask<object> TestAction()
{
    await Task.Yield();
    return "foo";
}
""";

            return new object[][]
            {
                new object[] { taskOfTMethod },
                new object[] { taskOfTWithYieldMethod },
                new object[] { taskOfObjectWithYieldMethod }
            };
        }
    }

    [Theory]
    [MemberData(nameof(ValueTaskOfTMethods))]
    public async Task CanInvokeFilter_OnValueTaskOfTReturningHandler(string innerSource)
    {
        // Arrange
        var source = $$"""
{{innerSource}}
app.MapGet("/", TestAction)
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        // Act
        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, "foo");
    }

    public static object[][] VoidReturningMethods
    {
        get
        {
            var voidMethod = "void TestAction() { }";

            var valueTaskMethod = """
ValueTask TestAction()
{
    return ValueTask.CompletedTask;
}
""";
            var taskMethod = """
Task TestAction()
{
    return Task.CompletedTask;
}
""";
            var valueTaskWithYieldMethod = """
async ValueTask TestAction()
{
    await Task.Yield();
}
""";

            var taskWithYieldMethod = """
async Task TestAction()
{
    await Task.Yield();
}
""";

            return new object[][]
            {
                new object[] { voidMethod },
                new object[] { valueTaskMethod },
                new object[] { taskMethod },
                new object[] { valueTaskWithYieldMethod },
                new object[] { taskWithYieldMethod}
            };
        }
    }

    [Theory]
    [MemberData(nameof(VoidReturningMethods))]
    public async Task CanInvokeFilter_OnVoidReturningHandler(string innerSource)
    {
        // Arrange
        var source = $$"""
{{innerSource}}
app.MapGet("/", TestAction)
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        // Act
        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, string.Empty);
    }

    [Theory]
    [MemberData(nameof(VoidReturningMethods))]
    public async Task CanInvokeFilter_OnVoidReturningHandler_WithModifyingResult(string innerSource)
    {
        // Arrange
        var source = $$"""
{{innerSource}}
app.MapGet("/", TestAction)
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    var result = await next(context);
    return $"Filtered: {result}";
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        // Act
        await endpoint.RequestDelegate(httpContext);

        await VerifyResponseBodyAsync(httpContext, "Filtered: Microsoft.AspNetCore.Http.HttpResults.EmptyHttpResult");
    }

    public static object[][] TasksOfTypesMethods
    {
        get
        {
            var valueTaskOfStructMethod = """
ValueTask<TodoStruct> TestAction()
{
    return ValueTask.FromResult(new TodoStruct { Name = "Test todo" });
}
""";

            var valueTaskOfStructWithYieldMethod = """
async ValueTask<TodoStruct> TestAction()
{
    await Task.Yield();
    return new TodoStruct { Name = "Test todo" };
}
""";

            var taskOfStructMethod = """
Task<TodoStruct> TestAction()
{
    return Task.FromResult(new TodoStruct { Name = "Test todo" });
}
""";

            var taskOfStructWithYieldMethod = """
async Task<TodoStruct> TestAction()
{
    await Task.Yield();
    return new TodoStruct { Name = "Test todo" };
}
""";

            return new object[][]
            {
                new object[] { valueTaskOfStructMethod },
                new object[] { valueTaskOfStructWithYieldMethod },
                new object[] { taskOfStructMethod },
                new object[] { taskOfStructWithYieldMethod }
            };
        }
    }

    [Theory]
    [MemberData(nameof(TasksOfTypesMethods))]
    public async Task CanInvokeFilter_OnHandlerReturningTasksOfStruct(string innerSource)
    {
        // Arrange
        var source = $$"""
{{innerSource}}
app.MapGet("/", TestAction)
.AddEndpointFilterFactory((routeHandlerContext, next) => async (context) =>
{
    return await next(context);
});
""";
        var (_, compilation) = await RunGeneratorAsync(source);
        var endpoint = GetEndpointFromCompilation(compilation);
        var httpContext = CreateHttpContext();

        // Act
        await endpoint.RequestDelegate(httpContext);

        // Assert
        await VerifyResponseJsonBodyAsync<TodoStruct>(httpContext, (todo) =>
        {
            Assert.Equal("Test todo", todo.Name);
        });
    }

    private class EndpointFeature : IEndpointFeature
    {
        public Endpoint Endpoint { get; set; }
    }
}
